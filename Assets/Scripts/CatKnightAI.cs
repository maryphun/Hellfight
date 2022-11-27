using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CatKnightAI : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private bool haveTurnAnimation;
    [SerializeField] private int attackDamageBase;
    [SerializeField] private int attackDamageMax;
    [SerializeField] private bool allowAttack;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackMoveDistance = 0.2f;
    [SerializeField] private float staminaRegenInterval;
    [SerializeField] private float dealDamageDelay;
    [SerializeField] private int initialStamina = 100;
    [SerializeField] private float turningTime = 1.0f;
    [SerializeField] private float dashRange = 3.0f;
    [SerializeField] private float abilityChargeTime = 1.9f;
    [SerializeField] private float abilityCooldownTime = 7f;

    private EnemyControl controller;

    [Header("References")]
    private Animator animator;
    private LayerMask playerLayer;
    private SpriteRenderer graphic;
    private string enemyName;
    private Controller player;
    private float afterImageInterval = 0.1f;
    [SerializeField] private GameObject abilityProgressBar;
    [SerializeField] private SpriteRenderer progressBarFill;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    [SerializeField] private int attackPattern = 0;
    [SerializeField] private float afterImageIntervalCount;
    [SerializeField, Range(0f,1f)] private float abilityChargeCnt;
    [SerializeField, Range(0f,7f)] private float abilityCooldownCnt;


    bool isDashing;
    float abilityChargeEffectInterval = 0.25f;
    float abilityChargeEffectCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;
    bool dealsecondDamageAlerady;
    bool isUsingAbility = false;
    private GameObject chargeEffect;
    private GameObject potionHealEffect;


    private void Awake()
    {
        potionHealEffect = Resources.Load("Prefabs/PotionHeal") as GameObject;
        chargeEffect = Resources.Load("Prefabs/Charge") as GameObject;
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

        player = FindObjectOfType<Controller>();

        controller.RegenStamina(initialStamina);

        SetScalingRule(controller.GetLevel());

        abilityProgressBar.SetActive(false); 
        isUsingAbility = false;
    }

    private void SetScalingRule(int level)
    {
        //attackDamageBase += level * 2;
        //attackDamageMax += level * (1 / 5);

        //controller.AddMaxHp(level * 5);
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
        abilityCooldownCnt = Mathf.Max(abilityCooldownCnt - Time.deltaTime, 0.0f);
        switch (controller.GetCurrentStatus())
        {
            case Status.Idle:
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
                TurnCtrl();
                break;
            case Status.Chasing:
                ChasingCtrl();
                break;
            case Status.Fleeing:
                FleeCtrl();
                break;
            case Status.SpecialAbility:
                SpecialAbilityCtrl();
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
                InitAttack();
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                if (isUsingAbility)
                {
                    animator.Play(enemyName + "Hit");
                    abilityProgressBar.SetActive(false);
                    isUsingAbility = false;
                    controller.UseAllStamina();
                }
                break;
            case Status.Turning:
                if (haveTurnAnimation)
                {
                    animator.Play(enemyName + "Turn");
                    statusTimer = controller.FindAnimation(animator, enemyName + "Turn").length - Time.fixedDeltaTime;
                }
                else
                {
                    animator.Play(enemyName + "Idle");
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
                StartDash();
                break;
            case Status.SpecialAbility:
                InitSpecialAbility();
                break;
            case Status.Dying:
                AudioManager.Instance.PlaySFX(enemyName + "Dead");
                break;
            default:
                InitStatus(Status.Idle);
                break;
        }
    }

    void InitAttack()
    {
        // get current attack pattern 1~3
        int currentAttackPattern = GetNextAttackPattern();

        // Animation
        animator.Play(enemyName + "Attack" + currentAttackPattern.ToString());
        statusTimer = controller.FindAnimation(animator, enemyName + "Attack" + currentAttackPattern.ToString()).length;

        // Delay damage deal and reset flags
        dealDamageCnt = 0.0f;
        dealDamageAlready = false;
        dealsecondDamageAlerady = false;

        // cost stamina
        controller.UseAllStamina();

        RaycastHit2D hitplayer = Physics2D.Raycast(transform.position, new Vector2(controller.GetDirectionInteger(), 0.0f), 
            controller.GetCollider().bounds.size.x * 0.5f, playerLayer);

        if (!hitplayer)
        {
            transform.DOMoveX(transform.position.x + controller.GetDirectionInteger() * attackMoveDistance, dealDamageDelay);
        }
    }

    void IdleCtrl()
    {
        // turn around if player at the opposite side
        if (!controller.IsFacingPlayer())
        {
            InitStatus(Status.Turning);
            return;
        }

        // MOVE
        int direction = graphic.flipX ? -1 : 1;
        if (controller.IsStaminaMax() && allowAttack && player.IsAlive()
            && Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange
            && GetComponent<Rigidbody2D>().velocity.y == 0.0f)
        {
            if (((float)controller.GetCurrentArmor() / (float)controller.GetMaxArmor()) < 0.5f)
            {
                InitStatus(Status.Fleeing);
            }
            else
            {
                InitStatus(Status.Attacking);
            }
            return;
        }
        else if (((float)controller.GetCurrentArmor() / (float)controller.GetMaxArmor()) <= 0.0f && Mathf.Abs(transform.position.x - player.transform.position.x) > 5f && abilityCooldownCnt <= 0.0f)
        {
            InitStatus(Status.SpecialAbility);
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
        }
    }

    void ChasingCtrl()
    {
        // turn around if player at the opposite side
        if ((player.transform.position.x > controller.transform.position.x
            && graphic.flipX)
            ||
            (player.transform.position.x < controller.transform.position.x
            && !graphic.flipX))
        {
            animator.SetBool("Move", false);
            InitStatus(Status.Turning);
            return;
        }

        int direction = graphic.flipX ? -1 : 1 ;

        // move toward player
        transform.DOMoveX(transform.position.x + moveSpeed * direction * Time.deltaTime, 0.0f, false);

        // reach destination
        if (Mathf.Abs(player.transform.position.x- transform.position.x ) < attackRange)
        {
            InitStatus(Status.Attacking);
        }
    }

    void AttackCtrl()
    {
        dealDamageCnt += Time.deltaTime;

        if (dealDamageCnt > dealDamageDelay && !dealDamageAlready)
        {
            // deal damage 
            float direction = controller.GetDirectionInteger();
            if (Mathf.Abs(player.transform.position.x -  (transform.position.x + (direction * attackRange / 2f))) < attackRange
                && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange/2f
                && controller.IsFacingPlayer())
            {
                player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax+1), transform);
            }
            AudioManager.Instance.PlaySFX("wind");
            dealDamageAlready = true;
        }

        // attack pattern 2 deal damage twice, the second damage deal behind him
        if (attackPattern == 2 && !dealsecondDamageAlerady && statusTimer < 1.25f)
        {
            // deal damage 
            float back = controller.GetDirectionInteger() * -1;
            if (Mathf.Abs(player.transform.position.x - (transform.position.x + controller.GetCollider().bounds.size.x)) < attackRange
                && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange / 2f
                && !controller.IsFacingPlayer())
            {
                player.DealDamage((attackDamageBase + Random.Range(0, attackDamageMax + 1)) / 4, transform);
            }
            // flag
            dealsecondDamageAlerady = true;
        }

        if (statusTimer == 0.0f)
        {
            if (attackPattern >= 3)
            {
                InitStatus(Status.Idle);
            }
            else
            {
                InitStatus(Status.Attacking);
            }
        }
    }

    void StartDash()
    {
        animator.Play(enemyName + "Dash");
        controller.SetInvulnerable(true);
        controller.SetImmumetoKnockback(true);
        statusTimer = controller.FindAnimation(animator, enemyName + "Dash").length;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(controller.GetDirectionInteger(), 0.0f), dashRange, player.GetFrameLayer());
        if (hit)
        {
            float newRange = Mathf.Abs(transform.position.x - (hit.point.x + (-controller.GetDirectionInteger() * controller.GetCollider().bounds.size.x)));
            transform.DOMoveX(hit.point.x + (-controller.GetDirectionInteger() * controller.GetCollider().bounds.size.x), statusTimer * (newRange / dashRange), false);
        }
        else
        {
            transform.DOMoveX(transform.position.x + (controller.GetDirectionInteger() * dashRange), statusTimer, false);
        }
        afterImageIntervalCount = afterImageInterval;
    }

    void FleeCtrl()
    {
        afterImageIntervalCount -= Time.deltaTime;
        if (afterImageIntervalCount <= 0.0f)
        {
            afterImageIntervalCount = afterImageInterval;
            controller.CreateAfterImage();
        }

        if (statusTimer == 0.0f)
        {
            if (!controller.IsFacingPlayer())
            {
                graphic.flipX = !graphic.flipX;
            }
            controller.SetInvulnerable(false);
            controller.SetImmumetoKnockback(false);
            InitStatus(Status.Attacking);
        }
    }    

    void InitSpecialAbility()
    {
        abilityProgressBar.SetActive(true); 
        isUsingAbility = true;
        progressBarFill.size = new Vector2(0f, progressBarFill.size.y);
        abilityChargeCnt = 0.0f;
        statusTimer = abilityChargeTime;
        animator.Play(enemyName + "Cast");
        abilityCooldownCnt = abilityCooldownTime;
    }

    void SpecialAbilityCtrl()
    {
        abilityChargeEffectCnt -= Time.deltaTime;
        if (abilityChargeEffectCnt <= 0.0f)
        {
            AudioManager.Instance.PlaySFX("Magic Cast 02", 0.8f);
            abilityChargeEffectCnt = abilityChargeEffectInterval;
            SpawnSpecialEffect(chargeEffect, new Vector2(transform.position.x, transform.position.y + 1.5f), 1.5f);
        }

        abilityChargeCnt = Mathf.Min(abilityChargeCnt + Time.deltaTime, abilityChargeTime);
        progressBarFill.size = new Vector2((abilityChargeCnt / abilityChargeTime) * 1.846f, progressBarFill.size.y);

        if (statusTimer <= 0.0f)
        {
            // finish
            AudioManager.Instance.PlaySFX("DefenseBuff", 1.5f);
            controller.RegeneraeArmor(controller.GetMaxArmor());
            SpawnSpecialEffect(potionHealEffect, new Vector2(transform.position.x, -1.46f));

            abilityProgressBar.SetActive(false);
            isUsingAbility = false;

            animator.Play(enemyName + "Idle");
            InitStatus(Status.Idle);
        }
    }

    private int GetNextAttackPattern()
    {
        if (attackPattern >= 3) attackPattern = 0;
        attackPattern++;

        return attackPattern;
    }

    GameObject SpawnSpecialEffect(GameObject prefab, Vector2 position, float scale = 1.0f)
    {
        GameObject tmp = Instantiate(prefab, position, Quaternion.identity);

        tmp.transform.DOScale(scale, 0.0f);
        tmp.transform.SetParent(transform);

        return tmp;
    }
}
