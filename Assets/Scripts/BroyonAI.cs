using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BroyonAI : MonoBehaviour
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
    private GameObject jumpDustEffect;

    [Header("Debug")]
    [SerializeField] private float statusTimer;

    float jumpCooldown = 4.0f;
    float jumpCdTimer;
    bool isDashing;
    float afterImgInterval = 0.2f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;

    private void Awake()
    {
        jumpDustEffect = Resources.Load("Prefabs/JumpDust") as GameObject;
    }

    // Update is called once per frame
    void Start()
    {
        controller = GetComponent<EnemyControl>();
        animator = controller.GetAnimator();
        playerLayer = controller.GetPlayerLayer();
        graphic = controller.GetGraphic();
        enemyName = controller.GetName();
        statusTimer = 0.0f;

        SetScalingRule(controller.GetLevel());
        AudioManager.Instance.PlaySFX(enemyName + "Dead");
    }

    private void SetScalingRule(int level)
    {
        attackDamageBase += level * 1;
        attackDamageMax += level * 2;
        moveSpeed += 0.05f * (float)level;
        staminaRegenInterval = staminaRegenInterval / ((float)level / 2);

        controller.AddMaxHp(level * 3);
    }

    private void FixedUpdate()
    {
        if (controller.IsPaused()) return;
        CheckFlags();

        jumpCdTimer = Mathf.Max(jumpCdTimer - Time.deltaTime, 0.0f);
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
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length + 0.1f;
                dealDamageCnt = 0.0f;
                dealDamageAlready = false;
                controller.UseAllStamina();
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                break;
            case Status.Turning:
                break;
            case Status.Chasing:
                AudioManager.Instance.PlaySFX(enemyName + "GonnaAttack");
                animator.Play(enemyName + "Move");
                animator.SetBool("Move", true);
                break;
            case Status.Fleeing:
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
        // turn around if controller.GetPlayer() at the opposite side
        if (haveTurnAnimation)
        {
            if ((controller.GetPlayer().transform.position.x > controller.transform.position.x
                && graphic.flipX)
                ||
                (controller.GetPlayer().transform.position.x < controller.transform.position.x
                && !graphic.flipX))
            {
                InitStatus(Status.Turning);
                return;
            }
        }

        // MOVE
        int direction = graphic.flipX ? -1 : 1;
        if (statusTimer == 0.0f && controller.IsStaminaMax() && controller.GetRigidBody().velocity.y == 0.0f && controller.GetPlayer().IsAlive())
        {
            InitStatus(Status.Chasing);
            return;
        }
        else if (controller.IsStaminaMax() && allowAttack && controller.GetPlayer().IsAlive() 
            && Mathf.Abs(controller.GetPlayer().transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange
            && controller.GetRigidBody().velocity.y == 0.0f)
        {
            InitStatus(Status.Attacking);
            return;
        }
        else if (!controller.IsStaminaMax() && Mathf.Abs(transform.position.x) > moveSpeed * Time.deltaTime)
        {
            InitStatus(Status.Fleeing);
            return;
        }

        // JUMP
        if (controller.GetCurrentHPPercentage() < 0.5f && controller.GetPlayer().IsJumping() && jumpCdTimer <= 0.0f)
        {
            StartJump();
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
        // turn around if controller.GetPlayer() at the opposite side
        if ((controller.GetPlayer().transform.position.x > controller.transform.position.x
            && graphic.flipX)
            ||
            (controller.GetPlayer().transform.position.x < controller.transform.position.x
            && !graphic.flipX))
        {
            graphic.flipX = !graphic.flipX;
            return;
        }

        int direction = graphic.flipX ? -1 : 1 ;

        // move toward controller.GetPlayer()
        transform.DOMoveX(transform.position.x + moveSpeed * direction * Time.deltaTime, 0.0f, false);

        // reach destination
        if (Mathf.Abs(controller.GetPlayer().transform.position.x - transform.position.x) < attackRange)
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Idle);
        }
        else
        {
            statusTimer = 1.0f;
        }
    }
    void FleeingCtrl()
    {
        // turn to target
        if ((0 > controller.transform.position.x
            && graphic.flipX)
            ||
            (0 < controller.transform.position.x
            && !graphic.flipX))
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Turning);
            return;
        }

        int direction = graphic.flipX ? -1 : 1;

        // reach destination
        if (Mathf.Abs(transform.position.x) < moveSpeed * Time.deltaTime && !controller.IsKnockbacking())
        {
            transform.DOMoveX(0.0f, 0.0f, false);
            animator.SetBool("Move", false);
            InitStatus(Status.Idle);
            return;
        }
        else
        {
            statusTimer = 1.0f;
        }

        // move
        float speedMultiplier = 1.0f;
        if (controller.GetRigidBody().velocity.y !=  0.0f)
        {
            // extra speed
            speedMultiplier = 5f;
        }
        transform.DOMoveX(transform.position.x + moveSpeed * direction * Time.deltaTime * speedMultiplier, 0.0f, false);

        // no need to flee anymore is stamina is max up
        if (controller.IsStaminaMax() && allowAttack && controller.GetPlayer().IsAlive()
            && Mathf.Abs(controller.GetPlayer().transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange
            && controller.GetRigidBody().velocity.y == 0.0f)
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Attacking);
            return;
        }

        // JUMP IF controller.GetPlayer() IS BLOCKING
        if (Mathf.Abs(controller.GetPlayer().transform.position.x - transform.position.x) < controller.GetCollider().bounds.size.x
            && controller.IsFacingPlayer()
            && jumpCdTimer <= 0.0f)
        {
            StartJump();
        }
    }

    void StartJump()
    {
        jumpCdTimer = jumpCooldown;

        // FX
        if (transform.position.y < -2f)
        {
            Transform tmp = Instantiate(jumpDustEffect, new Vector2(transform.position.x, -2.619f), Quaternion.identity).transform;
            tmp.transform.DOScale(2.0f, 0.0f);
        }

        // velocity
        controller.GetRigidBody().velocity = new Vector2(0.0f, 10.0f);
        controller.GetRigidBody().AddForce(new Vector2(0.0f, 60f));

        // SE
        AudioManager.Instance.PlaySFX("jump", 2.0f);
    }

    void AttackCtrl()
    {
        dealDamageCnt += Time.deltaTime;

        if (dealDamageCnt > dealDamageDelay && !dealDamageAlready)
        {
            // deal damage 
            float direction = graphic.flipX ? -1 : 1;
            if (Mathf.Abs(controller.GetPlayer().transform.position.x -  transform.position.x) < attackRange * 2f
                && Mathf.Abs(controller.GetPlayer().transform.position.y - transform.position.y) < attackRange
                && controller.IsFacingPlayer())
            {
                controller.GetPlayer().DealDamage(attackDamageBase + Random.Range(0, attackDamageMax+1), transform);
            }
            AudioManager.Instance.PlaySFX(enemyName + "Slash", 0.5f);
            dealDamageAlready = true;
        }

        if (statusTimer == 0.0f)
        {
            InitStatus(Status.Idle);
        }
    }

    public void CreateAfterImage()
    {
        //--- spawning new empty object, copying tranform ---
        GameObject afterImg = new GameObject("afterImg");
        afterImg.transform.position = graphic.transform.position;
        afterImg.transform.rotation = graphic.transform.rotation;
        afterImg.transform.localScale = graphic.transform.localScale;
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
}
