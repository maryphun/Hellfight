using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CultistAI : EnemyAI
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

    private EnemyControl controller;

    [Header("References")]
    private Animator animator;
    private LayerMask playerLayer;
    private SpriteRenderer graphic;
    private string enemyName;
    private Controller player;
    [SerializeField] private GameObject burstEffect;
    [SerializeField] private GameObject fireBurstHitEffect;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    
    bool isDashing;
    float afterImgInterval = 0.2f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;

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

        controller.RegenStamina(initialStamina);

        SetScalingRule(controller.GetLevel());
    }

    private void SetScalingRule(int level)
    {
        // cultist's damage doesn't scale.

        controller.AddMaxHp(level * 4);
        if (level > 30)
        {
            controller.AddMaxHp(50);
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
                animator.SetBool("Move", false);
                IdleCtrl();
                break;
            case Status.Attacking:
                AttackCtrl();
                break;
            case Status.Attacked:
                // stunned
                if (statusTimer <= 0.0f)
                {
                    InitStatus(Status.Idle);
                }
                break;
            case Status.Turning:
                animator.SetBool("Move", false);
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
                statusTimer = 0.01f; // a delay before the next action
                break;
            case Status.Attacking:
                //AudioManager.Instance.PlaySFX("heartbeat");
                animator.Play(enemyName + "Attack");
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length;
                dealDamageCnt = 0.0f;
                dealDamageAlready = false;
                controller.UseAllStamina();
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                break;
            case Status.Turning:
                if (haveTurnAnimation)
                {
                    animator.Play(enemyName + "Turn");
                    statusTimer = controller.FindAnimation(animator, enemyName + "Turn").length - Time.fixedDeltaTime;
                }
                else
                {
                    statusTimer = turningTime;
                }
                break;
            case Status.Chasing:
                if (!controller.IsFacingPlayer())
                {
                    InitStatus(Status.Turning);
                    return;
                }
                isDashing = true;
                animator.Play(enemyName + "Move");
                animator.SetBool("Move", true);
                break;
            case Status.Fleeing:
                break;
            case Status.SpecialAbility:
                break;
            case Status.Dying:
                AudioManager.Instance.PlaySFX("dead");
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
        int direction = graphic.flipX ? -1 : 1;
        if (controller.IsStaminaMax() && allowAttack && player.IsAlive()
            && Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange
            && GetComponent<Rigidbody2D>().velocity.y == 0.0f && controller.IsInScreen())
        {
            InitStatus(Status.Attacking);
            return;
        }
        else if (statusTimer == 0.0f && controller.IsStaminaMax() && GetComponent<Rigidbody2D>().velocity.y == 0.0f && player.IsAlive())
        {
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
        if (statusTimer <= 0.0f)
        {
            InitStatus(Status.Idle);
            graphic.flipX = !graphic.flipX;
            AudioManager.Instance.PlaySFX(enemyName + "GonnaAttack");
        }
    }

    void ChasingCtrl()
    {
        // turn around if player at the opposite side
        if (!controller.IsFacingPlayer())
        {
            InitStatus(Status.Turning);
            return;
        }

        int direction = graphic.flipX ? -1 : 1 ;

        // move toward player
        transform.DOMoveX(transform.position.x + moveSpeed * direction * Time.deltaTime, 0.0f, false);

        // reach destination
        if (Mathf.Abs(player.transform.position.x- transform.position.x ) < attackRange
            && controller.IsInScreen())
        {
            InitStatus(Status.Attacking);
        }
    }

    void AttackCtrl()
    {
        dealDamageCnt += Time.deltaTime;

        if (!dealDamageAlready && statusTimer < dealDamageDelay)
        {
            StartCoroutine(HeavensFury());
            dealDamageAlready = true;
        }

        if (statusTimer == 0.0f)
        {
            InitStatus(Status.Idle);
        }
    }

    IEnumerator HeavensFury()
    {
        GameObject effect = controller.SpawnSpecialEffect(burstEffect, new Vector2(player.transform.position.x, -3.09f), false);

        AudioManager.Instance.PlaySFX("helljump");
        yield return new WaitForSeconds(0.6f);
        AudioManager.Instance.PlaySFX("burst");

        // calculate dealt damage and heal cultist
        int dealtDamage = 0;

        // deal damage to player
        if (Mathf.Abs(player.transform.position.x - effect.transform.position.x) < 1.9f)
        {
            int calculatedDamage = attackDamageBase + Random.Range(0, attackDamageMax + 1);
            if (player.DealDamage(calculatedDamage, transform))
            {
                dealtDamage += calculatedDamage;
                player.StartJump(false, true);
                GameObject tmp = Instantiate(fireBurstHitEffect, player.transform.position, Quaternion.identity);
                tmp.transform.SetParent(player.transform.parent);
            }
        }

        // deal damage to enemy
        List<EnemyControl> enemyList = controller.GetGameManager().GetMonsterList();

        if (enemyList.Count > 0)
        {
            foreach (EnemyControl enemy in enemyList)
            {
                if (Mathf.Abs(enemy.transform.position.x - effect.transform.position.x) < 1.9f + enemy.GetCollider().bounds.size.x / 2f
                        && enemy.transform.position.y < 2f && enemy.GetName() != "Cultist")
                {
                    int calculatedDamage = attackDamageBase / 2 + Random.Range(0, attackDamageMax / 2 + 1);
                    if (enemy.DealDamage(calculatedDamage))
                    {
                        dealtDamage += calculatedDamage;

                        GameObject tmp = Instantiate(fireBurstHitEffect, enemy.transform.position, Quaternion.identity);
                        tmp.transform.SetParent(enemy.transform.parent);

                        Vector2 randomize = new Vector2(Random.Range(-enemy.GetComponent<Collider2D>().bounds.size.x / 2f, enemy.GetComponent<Collider2D>().bounds.size.x / 2f), Random.Range(-0.5f, 0.5f));
                        Vector2 floatDirection = new Vector2(0.0f, 1.0f);
                        controller.GetGameManager().SpawnFloatingText(new Vector2(enemy.transform.position.x, enemy.transform.position.y + enemy.GetComponent<Collider2D>().bounds.size.y * 0.75f) + randomize
                                                     , 2f + Random.Range(0.0f, 1.0f), 25f + Random.Range(0.0f, 25.0f),
                                                     calculatedDamage.ToString(), Color.white, floatDirection.normalized, 50f);
                    }
                }
            }
        }

        if (dealtDamage > 0)
        {
            // heal cultist
            controller.Heal(dealtDamage * 2);
        }
    }
}
