using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class JinAI : EnemyAI
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
    private GameObject chargeEffect;
    private GameObject fireburstEffect;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    
    bool isDashing;
    float afterImgInterval = 0.05f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;
    bool startedCharge;
    GameObject chargeEffectReference;

    float targetMoveX;

    private void Awake()
    {
        fireburstEffect = Resources.Load("Prefabs/JinBurst") as GameObject;
        chargeEffect = Resources.Load("Prefabs/Charge") as GameObject;
    }
   
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

        controller.RegenStamina(initialStamina);
    }

    private void SetScalingRule(int level)
    {
        attackDamageBase += (int)((float)level * 1.75f);
        attackDamageMax += level / 2;
        moveSpeed += 0.05f * (float)level;
        //staminaRegenInterval = staminaRegenInterval / ((float)level / 2);

        controller.AddMaxHp(level * 5);
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
                animator.Play(enemyName + "Attack");
                AudioManager.Instance.PlaySFX("KobrodCharging", 1f);
                AudioManager.Instance.PlaySFX("heartbeat");
                SpawnSpecialEffect();
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length + Time.deltaTime;
                dealDamageCnt = 0.0f;
                startedCharge = false;
                dealDamageAlready = false;
                controller.UseAllStamina();
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                controller.RegenStamina(5);
                break;
            case Status.Turning:
                statusTimer = turningTime;
                break;
            case Status.Chasing:
                animator.Play(enemyName + "Move");
                animator.SetBool("Move", true);
                break;
            case Status.Fleeing:
                targetMoveX = Random.Range(-8f, 8f);
                animator.Play(enemyName + "Move");
                animator.SetBool("Move", true);
                break;
            case Status.SpecialAbility:
                break;
            case Status.Dying:
                AudioManager.Instance.PlaySFX("burst", 0.5f);
                AudioManager.Instance.PlaySFX("JinDeath", 0.75f);
                break;
            default:
                InitStatus(Status.Idle);
                break;
        }

        // charge got distrupted. Remove the charge effect so it's not confusing to the player.
        if (newStatus != Status.Attacking)
        {
            if (!ReferenceEquals(chargeEffectReference, null))
            {
                Destroy(chargeEffectReference);
            }
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
            && Mathf.Abs(targetMoveX - transform.position.x) < controller.GetCollider().bounds.size.x / 2f + attackRange && GetComponent<Rigidbody2D>().velocity.y == 0)
        {
            InitStatus(Status.Attacking);
            return;
        }
        else if (!controller.IsStaminaMax())
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
        if (Mathf.Abs(targetMoveX - transform.position.x) < controller.GetCollider().bounds.size.x / 2f && !controller.IsKnockbacking())
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
            && Mathf.Abs(targetMoveX - transform.position.x) < controller.GetCollider().bounds.size.x / 2f && GetComponent<Rigidbody2D>().velocity.y == 0)
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Attacking);
            return;
        }
    }

    void FleeingCtrl()
    {
        // reach destination
        if (Mathf.Abs(transform.position.x - targetMoveX) < moveSpeed * Time.deltaTime)
        {
            // give new destination
            targetMoveX = Random.Range(-8f, 8f);
        }

        // turn
        if (transform.position.x > targetMoveX && !graphic.flipX)
        {
            graphic.flipX = true;
        }
        if (transform.position.x < targetMoveX && graphic.flipX)
        {
            graphic.flipX = false;
        }

        // move
        int direction = graphic.flipX ? -1 : 1;
        transform.position = new Vector2(transform.position.x + direction * moveSpeed * Time.deltaTime, transform.position.y);

        // goooo booom stop running around
        if (controller.IsStaminaMax())
        {
            InitStatus(Status.Idle);
        }
    }

    float CalculateTargetMoveX()
    {
        int direction = transform.position.x > player.transform.position.x ? 1 : -1;
        return player.transform.position.x + (direction * controller.GetCollider().bounds.size.x/ 2f);
    }

    void AttackCtrl()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(enemyName + "Attack"))
        {
            InitStatus(Status.Idle);
        }

        dealDamageCnt += Time.deltaTime;
        if (dealDamageCnt > dealDamageDelay)
        {
            if (!startedCharge)
            {
                controller.SetImmumetoKnockback(true);
                controller.UseAllStamina();
                startedCharge = true;

                // FX
                GameObject tmp = Instantiate(fireburstEffect, transform.position, Quaternion.identity);
                tmp.transform.SetParent(transform.parent);
                AudioManager.Instance.PlaySFX("burst", 0.65f);

                // left
                tmp = Instantiate(fireburstEffect, new Vector2(transform.position.x - controller.GetCollider().bounds.size.x, transform.position.y), Quaternion.identity);
                tmp.transform.SetParent(transform.parent);
                // right
                tmp = Instantiate(fireburstEffect, new Vector2(transform.position.x + controller.GetCollider().bounds.size.x, transform.position.y), Quaternion.identity);
                tmp.transform.SetParent(transform.parent);
                // top
                tmp = Instantiate(fireburstEffect, new Vector2(transform.position.x, transform.position.y + controller.GetCollider().bounds.size.y), Quaternion.identity);
                tmp.transform.SetParent(transform.parent);
                // top left
                tmp = Instantiate(fireburstEffect, new Vector2(transform.position.x - controller.GetCollider().bounds.size.x, transform.position.y + controller.GetCollider().bounds.size.y / 2f), Quaternion.identity);
                tmp.transform.SetParent(transform.parent);
                // top right
                tmp = Instantiate(fireburstEffect, new Vector2(transform.position.x + controller.GetCollider().bounds.size.x, transform.position.y + controller.GetCollider().bounds.size.y / 2f), Quaternion.identity);
                tmp.transform.SetParent(transform.parent);

                // DEALDAMAGE
                if (!dealDamageAlready && Mathf.Abs(player.transform.position.x - transform.position.x) < attackRange
                                       && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange * 2f)
                {
                    player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform);
                    dealDamageAlready = true;
                }
                else
                {
                    AudioManager.Instance.PlaySFX("JinAttackMiss", 0.5f);
                }
            }
        }

        if (statusTimer == 0.0f)
        {
            controller.SetImmumetoKnockback(false);
            InitStatus(Status.Idle);
        }
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
        chargeEffectReference = Instantiate(chargeEffect, transform.position, Quaternion.identity);
        chargeEffectReference.transform.DOScale(1.5f, 0.0f);
        chargeEffectReference.transform.SetParent(transform);
    }
}
