using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class KobrodAI : EnemyAI
{
    [Header("Setting")]
    [SerializeField] private bool haveTurnAnimation;
    [SerializeField] private int attackDamageBase;
    [SerializeField] private int attackDamageMax;
    [SerializeField] private bool allowAttack;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float staminaRegenInterval;
    [SerializeField] private float dealDamageDelay;
    [SerializeField] private int initialStamina = 100;
    [SerializeField] private float turningTime = 1.0f;
    [SerializeField] private float idleDelay = 0.8f;

    private EnemyControl controller;

    [Header("References")]
    private Animator animator;
    private LayerMask playerLayer;
    private SpriteRenderer graphic;
    private string enemyName;
    private Controller player;
    [SerializeField] private GameObject chargeEffect;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    
    bool isDashing;
    float afterImgInterval = 0.05f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;
    bool startedCharge;

    float targetMoveX;

    // Update is called once per frame
    void Start()
    {
        controller = GetComponent<EnemyControl>();
        animator = controller.GetAnimator();
        playerLayer = controller.GetPlayerLayer();
        graphic = controller.GetGraphic();
        enemyName = controller.GetName();
        statusTimer = 0.0f;

        player = FindObjectOfType<Controller>();

        SetScalingRule(controller.GetLevel());
    }

    private void SetScalingRule(int level)
    {
        attackDamageBase += (int)((float)level * 1.75f);
        attackDamageMax += level / 2;
        moveSpeed += 0.05f * (float)level;
        //staminaRegenInterval = staminaRegenInterval / ((float)level / 2);

        controller.AddMaxHp(level * 5);

        if (level > 10)
        {
            controller.AddMaxHp(25);
        }
        if (level > 20)
        {
            controller.AddMaxHp(20);
            attackDamageBase += Random.Range(2, 5);
        }
    }

    private void FixedUpdate()
    {
        if (controller.IsPaused()) return;
        CheckFlags();

        statusTimer = Mathf.Max(statusTimer - Time.deltaTime, 0.0f);
        switch (controller.GetCurrentStatus())
        {
            case Status.Idle:
                IdleCtrl();
                RegenerateStamina();
                break;
            case Status.Attacking:
                AttackCtrl();
                break;
            case Status.Attacked:
                AttackedCtrl();
                break;
            case Status.Turning:
                TurnCtrl();
                break;
            case Status.Chasing:
                ChasingCtrl();
                break;
            case Status.Fleeing:
                FleeingCtrl();
                RegenerateStamina();
                break;
            case Status.SpecialAbility:
                break;
            case Status.Dying:
                break;
            default:
                InitStatus(Status.Idle);
                break;
        }
    }

    void CheckFlags()
    {

        if (controller.IsCriticalHitted())
        {
            // critical hit detected
            controller.ResetCriticalHitFlag();
        }

        if (controller.IsStatusChanged())
        {
            // status changes detected
            InitStatus(controller.GetCurrentStatus());
            controller.ResetStatusChanged();
        }

    }

    private void InitStatus(Status newStatus)
    {
        controller.SetCurrentStatus(newStatus);
        switch (newStatus)
        {
            case Status.Idle:
                statusTimer = idleDelay; // a delay before the next action
                break;
            case Status.Attacking:
                // turn to target
                if ((transform.position.x > player.transform.position.x
                    && !graphic.flipX)
                    ||
                    (transform.position.x < player.transform.position.x
                    && graphic.flipX))
                {
                    graphic.flipX = !graphic.flipX;
                }
                animator.Play(enemyName + "Attack");
                AudioManager.Instance.PlaySFX(enemyName + "Charging", 1f);
                SpawnSpecialEffect();
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length + Time.deltaTime;
                dealDamageCnt = 0.0f;
                startedCharge = false;
                dealDamageAlready = false;
                controller.UseAllStamina();
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                controller.RegenStamina(100);
                break;
            case Status.Turning:
                statusTimer = turningTime;
                break;
            case Status.Chasing:
                targetMoveX = CalculateTargetMoveX();
                animator.Play(enemyName + "Move");
                animator.SetBool("Move", true);
                break;
            case Status.Fleeing:
                targetMoveX = CalculateTargetMoveX();
                animator.Play(enemyName + "Move");
                animator.SetBool("Move", true);
                break;
            case Status.SpecialAbility:
                break;
            case Status.Dying:
                AudioManager.Instance.PlaySFX(enemyName + "Dead");
                break;
            default:
                InitStatus(Status.Idle);
                break;
        }
    }

    void IdleCtrl()
    {
        targetMoveX = CalculateTargetMoveX();
        // turn around if player at the opposite side
        if (haveTurnAnimation)
        {
            if ((targetMoveX > controller.transform.position.x
                && graphic.flipX)
                ||
                (targetMoveX < controller.transform.position.x
                && !graphic.flipX))
            {
                InitStatus(Status.Turning);
                return;
            }
        }

        // MOVE
        int direction = graphic.flipX ? -1 : 1;
        if (statusTimer == 0.0f && controller.IsStaminaMax() && Mathf.Abs(transform.position.x - targetMoveX) > 1.0f && player.IsAlive())
        {
            InitStatus(Status.Chasing);
            return;
        }
        else if (controller.IsStaminaMax() && allowAttack && player.IsAlive() 
            && Mathf.Abs(transform.position.x - targetMoveX) < 1.0f && GetComponent<Rigidbody2D>().velocity.y == 0)
        {
            InitStatus(Status.Attacking);
            return;
        }
        else if (!controller.IsStaminaMax() && Mathf.Abs(transform.position.x - targetMoveX) > 2.1f)
        {
            InitStatus(Status.Fleeing);
            return;
        }
    }

    void TurnCtrl()
    {
        if (statusTimer == 0.0f)
        {
            InitStatus(Status.Idle);
            graphic.flipX = !graphic.flipX;
        }
    }

    void AttackedCtrl()
    {
        // stunned
        if (statusTimer <= 0.0f)
        {
            InitStatus(Status.Idle);
        }
    }

    void ChasingCtrl()
    {
        // constantly calculate destination
        targetMoveX = CalculateTargetMoveX();

        // turn to target
        if ((transform.position.x > targetMoveX
            && !graphic.flipX)
             ||
            (transform.position.x < targetMoveX
            && graphic.flipX))
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Turning);
            return;
        }

        int direction = graphic.flipX ? -1 : 1;

        // reach destination
        if (Mathf.Abs(targetMoveX - transform.position.x) < moveSpeed * Time.deltaTime && !controller.IsKnockbacking())
        {
            transform.DOMoveX(targetMoveX, 0.0f, false);
            animator.SetBool("Move", false);
            InitStatus(Status.Idle);
            return;
        }
        else
        {
            statusTimer = 1.0f;
        }

        // move
        transform.DOMoveX(transform.position.x + moveSpeed * direction * Time.deltaTime, 0.0f, false);

        // no need to flee anymore is stamina is max up
        if (controller.IsStaminaMax() && allowAttack && player.IsAlive()
            && Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange && GetComponent<Rigidbody2D>().velocity.y == 0)
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Attacking);
            return;
        }
    }

    void FleeingCtrl()
    {
        targetMoveX = CalculateTargetMoveX();
        // turn to target
        if ((transform.position.x > targetMoveX
            && !graphic.flipX)
            ||
            (transform.position.x < targetMoveX
            && graphic.flipX))
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Turning);
            return;
        }

        int direction = graphic.flipX ? -1 : 1;

        // reach destination
        if (Mathf.Abs(transform.position.x - targetMoveX) < 1.0f && !controller.IsKnockbacking())
        {
            // turn to player
            if ((transform.position.x > player.transform.position.x
                && !graphic.flipX)
                ||
                (transform.position.x < player.transform.position.x
                && graphic.flipX))
            {
                graphic.flipX = !graphic.flipX;
                return;
            }

            animator.SetBool("Move", false);
            InitStatus(Status.Idle);
            return;
        }
        else
        {
            statusTimer = 1.0f;
        }

        // move
        transform.DOMoveX(transform.position.x + moveSpeed * direction * Time.deltaTime, 0.0f, false);

        // no need to flee anymore is stamina is max up
        if (controller.IsStaminaMax() && allowAttack && player.IsAlive() && Mathf.Abs(transform.position.x - targetMoveX) < 0.5f && GetComponent<Rigidbody2D>().velocity.y == 0.0f)
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Attacking);
            return;
        }
    }

    float CalculateTargetMoveX()
    {
        int fleedirection = (transform.position.x > player.transform.position.x) ? 1 : -1;

        //Debug.Log(player.transform.position.x + (fleedirection * (attackRange * 2.5f)));
        return Mathf.Clamp(player.transform.position.x + (fleedirection * (attackRange * 2.5f)), -9.5f, 9.5f);
    }

    void AttackCtrl()
    {
        dealDamageCnt += Time.deltaTime;

        if (dealDamageCnt > dealDamageDelay)
        {
            if (!startedCharge)
            {
                AudioManager.Instance.PlaySFX(enemyName + "Charge", 0.75f);
                controller.SetImmumetoKnockback(true);
                controller.UseAllStamina();
                startedCharge = true;
                animator.Play(enemyName + "Attack", 0, dealDamageDelay / controller.FindAnimation(animator, enemyName + "Attack").length);
            }

            // deal damage 
            float direction = graphic.flipX ? -1 : 1;
            if (!dealDamageAlready && Mathf.Abs(player.transform.position.x -  (transform.position.x + (direction * attackRange / 2f))) < attackRange
                && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange * 1.2f)
            {
                if (player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax+1), transform))
                {
                    AudioManager.Instance.PlaySFX(enemyName + "ChargeDamage", 1f);
                }
                dealDamageAlready = true;
            }

            // dash toward
            transform.position = new Vector2(transform.position.x + direction * (moveSpeed * 3f) * Time.deltaTime, transform.position.y);

            // offscreen
            if (   (transform.position.x > 8.5f && direction == 1) 
                || (transform.position.x < -8.5f && direction == -1))
            {
                statusTimer = 0.0f;
            }

            // after image
            afterImgCnt -= Time.deltaTime;
            if (afterImgCnt < 0.0f)
            {
                afterImgCnt = afterImgInterval;
                CreateAfterImage();
            }
        }

        if (statusTimer == 0.0f)
        {
            controller.SetImmumetoKnockback(false);
            InitStatus(Status.Idle);
        }
    }

    public void CreateAfterImage()
    {
        //--- spawning new empty object, copying tranform ---
        GameObject afterImg = new GameObject("afterImg");
        afterImg.transform.position = graphic.transform.position;
        afterImg.transform.rotation = graphic.transform.rotation;
        afterImg.transform.localScale = graphic.transform.lossyScale;
        afterImg.gameObject.layer = 0;
        //--- copying spriterenderer ---
        SpriteRenderer tailRenderer = afterImg.AddComponent<SpriteRenderer>();
        SpriteRenderer originalRenderer = graphic.GetComponent<SpriteRenderer>();
        tailRenderer.sortingOrder = originalRenderer.sortingOrder - 1;
        tailRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        tailRenderer.sprite = originalRenderer.sprite;
        tailRenderer.color = originalRenderer.color;
        tailRenderer.flipX = originalRenderer.flipX;
        tailRenderer.material = originalRenderer.material;
        //tailRenderer.material = originalMaterial;
        //--- initiating tail ---
        afterImg.AddComponent<Tail>();
        afterImg.GetComponent<Tail>().Initialization(0.5f, tailRenderer, 0.5f);
        //--- done ---
        Destroy(afterImg, 0.5f);
    }

    void RegenerateStamina()
    {
        staminaRegenCnt -= Time.deltaTime;
        if (staminaRegenCnt < 0.0f)
        {
            staminaRegenCnt = staminaRegenInterval;
            controller.RegenStamina(1);
        }
    }

    void SpawnSpecialEffect()
    {
        int direction = graphic.flipX ? -1 : 1;
        GameObject tmp = Instantiate(chargeEffect, new Vector2(transform.position.x + (direction * attackRange / 2f), transform.position.y), Quaternion.identity);
        tmp.transform.SetParent(transform);
    }
}
