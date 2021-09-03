using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum Status
{
    Idle,
    Attacking,
    Attacked,
    Turning,
    Chasing,
    Fleeing,
    SpecialAbility,
    Dying,
}

public class JellySlimeAI : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private bool haveTurnAnimation;
    [SerializeField] private int attackDamageBase;
    [SerializeField] private int attackDamageMax;
    [SerializeField] private bool allowAttack;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float staminaRegenInterval;
    [SerializeField] private float dealDamageInterval;

    private EnemyControl controller;

    [Header("References")]
    private Animator animator;
    private LayerMask playerLayer;
    private SpriteRenderer graphic;
    private string enemyName;
    private Controller player;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    
    bool isDashing;
    float afterImgInterval = 0.2f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;

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
        attackDamageBase += level / 4;
        moveSpeed += 0.1f * (float)level + Random.Range(-0.5f, 0.5f);

        controller.AddMaxHp(level * 4);

        if (level > 10)
        {
            controller.AddMaxHp(30);
            attackDamageBase += Random.Range(1,3);
        }
    }

    private void FixedUpdate()
    {
        if (controller.IsPaused()) return;

        if (controller.IsStatusChanged())
        {
            InitStatus(controller.GetCurrentStatus());
            controller.ResetStatusChanged();
        }

        statusTimer = Mathf.Max(statusTimer - Time.deltaTime, 0.0f);
        switch (controller.GetCurrentStatus())
        {
            case Status.Idle:
                IdleCtrl();
                break;
            case Status.Attacking:
                break;
            case Status.Attacked:
                // stunned
                if (statusTimer <= 0.0f)
                {
                    InitStatus(Status.Idle);
                }
                break;
            case Status.Turning:
                TurnCtrl();
                break;
            case Status.Chasing:
                ChasingCtrl();
                break;
            case Status.Fleeing:
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

    private void InitStatus(Status newStatus)
    {
        controller.SetCurrentStatus(newStatus);
        switch (newStatus)
        {
            case Status.Idle:
                statusTimer = 0.5f; // delay 0.5sec then moveon to next action
                break;
            case Status.Attacking:
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                break;
            case Status.Turning:
                animator.Play(enemyName + "Turn");
                statusTimer = controller.FindAnimation(animator, enemyName + "Turn").length - Time.fixedDeltaTime;
                break;
            case Status.Chasing:
                isDashing = true;
                dealDamageCnt = dealDamageInterval;
                controller.SetImmumetoKnockback(true);
                animator.Play(enemyName + "Dash");
                statusTimer = controller.FindAnimation(animator, enemyName + "Dash").length - Time.fixedDeltaTime;
                break;
            case Status.Fleeing:
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
        // turn around if player at the opposite side
        if (haveTurnAnimation)
        {
            if ((player.transform.position.x > controller.transform.position.x
                && graphic.flipX)
                ||
                (player.transform.position.x < controller.transform.position.x
                && !graphic.flipX))
            {
                InitStatus(Status.Turning);
                return;
            }
        }

        // MOVE
        if (statusTimer == 0.0f && controller.IsStaminaMax() && GetComponent<Rigidbody2D>().velocity.y == 0.0f && allowAttack && player.IsAlive())
        {
            controller.UseAllStamina();
            InitStatus(Status.Chasing);
            return;
        }

        staminaRegenCnt -= Time.deltaTime;
        if (staminaRegenCnt < 0.0f)
        {
            staminaRegenCnt = staminaRegenInterval;
            controller.RegenStamina(1);
        }
    }

    void TurnCtrl()
    {
        if (statusTimer == 0.0f)
        {
            animator.Play(enemyName + "Idle");
            InitStatus(Status.Idle);
            graphic.flipX = !graphic.flipX;
        }
    }

    void ChasingCtrl()
    {
        // landed
        if (isDashing && statusTimer == 0.0f)
        {
            isDashing = false;
            controller.SetImmumetoKnockback(false);
            InitStatus(Status.Idle);
            return;
        }

        // still dashing
        if (isDashing)
        {
            // movement
            int direction = graphic.flipX ? -1 : 1;
            transform.position = new Vector2(transform.position.x + direction * moveSpeed * Time.deltaTime, transform.position.y);
            
            // after image
            afterImgCnt -= Time.deltaTime;
            if (afterImgCnt < 0.0f)
            {
                AudioManager.Instance.PlaySFX(enemyName + "Attack");
                afterImgCnt = afterImgInterval;
                controller.CreateAfterImage();
            }

            // deal damage 
            dealDamageCnt -= Time.deltaTime;
            if (dealDamageCnt < 0.0f)
            {
                if (Vector2.Distance(player.transform.position, transform.position ) < attackRange)
                {
                    dealDamageCnt = dealDamageInterval;
                    player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform);
                }
            }
        }
    }
}
