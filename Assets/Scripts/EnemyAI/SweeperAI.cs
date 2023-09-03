using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SweeperAI : EnemyAI
{
    enum AttackType
    {
        Slam,
        Sweep,
        Max,
    }

    [Header("Setting")]
    [SerializeField] private bool haveTurnAnimation;
    [SerializeField] private int attackDamageBase;
    [SerializeField] private int attackDamageMax;
    [SerializeField] private bool allowAttack;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float staminaRegenInterval;
    [SerializeField] private int initialStamina = 100;
    [SerializeField] private float turningTime = 1.0f;

    [Header("SpecialSetting")]
    [SerializeField] private float rangeToAttack = 5.0f;
    [SerializeField] private float dealDamageDelayAttack1 = 0.28f;
    [SerializeField] private float dealDamageDelayAttack2 = 0.25f;
    [SerializeField] private float dashRangeAttack1 = 0.0f;
    [SerializeField] private float dashRangeAttack2 = 4.5f;
    [SerializeField] private float darkTrackDistance = 1.0f;

    private EnemyControl controller;

    [Header("References")]
    [SerializeField] private GameObject sweepEffect;
    [SerializeField] private GameObject skullEffect;
    [SerializeField] private GameObject trackEffect;
    private Animator animator;
    private LayerMask playerLayer;
    private SpriteRenderer graphic;
    private string enemyName;
    private Controller player;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    [SerializeField] private AttackType attackType;
    
    bool isDashing;
    float afterImgInterval = 0.2f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;
    bool attackDashAlready;

    // sweep attack related
    const float darkpactLocationY = -2.992f;
    float startPoint, endpoint, lastFramePos;
    List<GameObject> tracks = new List<GameObject>();

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
        attackDamageBase += level * 1;
        moveSpeed += Random.Range(-0.5f, 0.5f); ;

        controller.AddMaxHp(level * 5);

        if (level > 10)
        {
            controller.AddMaxHp(20);
            attackDamageBase += Random.Range(1, 3);
        }
        if (level > 20)
        {
            controller.AddMaxHp(25);
            attackDamageBase += Random.Range(2, 4);
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
                {
                    controller.TurnTowardPlayer();
                    DecideAttackType();
                    //AudioManager.Instance.PlaySFX("heartbeat");
                    animator.Play(GetAttackAnimationName(attackType));
                    statusTimer = controller.FindAnimation(animator, GetAttackAnimationName(attackType)).length;
                    dealDamageCnt = 0.0f;
                    dealDamageAlready = false;
                    attackDashAlready = false;
                    controller.UseAllStamina();

                    // spawn effect
                    if (attackType == AttackType.Sweep)
                    {
                        int direction = graphic.flipX ? -1 : 1;
                        controller.SpawnSpecialEffect(skullEffect, new Vector2(transform.position.x + (-direction * attackRange / 3f), transform.position.y), false);
                    }

                    // setup after bursts
                    startPoint = transform.position.x;
                    lastFramePos = transform.position.x;

                    AudioManager.Instance.PlaySFX("SweeperReady", 2.0f);
                    break;
                }
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
                AudioManager.Instance.PlaySFX("SweeperAwaken", 1.0f);
                break;
            case Status.Fleeing:
                break;
            case Status.SpecialAbility:
                break;
            case Status.Dying:
                AudioManager.Instance.PlaySFX(enemyName + "Dead", 0.4f);
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
            && Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * rangeToAttack / 2f))) < rangeToAttack
            && GetComponent<Rigidbody2D>().velocity.y == 0.0f)
        {
            // only make this decision if enemy is in screen and not on air
            if (   ((transform.position.x < 8.5f && direction == 1) 
                || (transform.position.x > -8.5f && direction == -1))
                && IsOnGround())
            {
                InitStatus(Status.Attacking);
            }
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
        if (Mathf.Abs(player.transform.position.x- transform.position.x ) < rangeToAttack)
        {
            // only make this decision if enemy is in screen and not on air
            if (   ((transform.position.x < 8.5f && direction == 1) 
                || (transform.position.x > -8.5f && direction == -1))
                && IsOnGround())
            {
                InitStatus(Status.Attacking);
            }
        }
    }

    void AttackCtrl()
    {
        dealDamageCnt += Time.deltaTime;
        float direction = graphic.flipX ? -1 : 1;
        
        if (dealDamageCnt > GetAttackDealDamageDelay(attackType))
        {
            if (attackType == AttackType.Slam && !dealDamageAlready)
            {
                // deal damage 
                if (Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange
                    && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange / 2f
                    && ((player.transform.position.x > transform.position.x && !graphic.flipX) || (player.transform.position.x < transform.position.x && graphic.flipX)))
                {
                    player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform);
                }
                dealDamageAlready = true;
            }
            else if (attackType == AttackType.Sweep)
            {
                if (!dealDamageAlready && statusTimer > 0.15f)
                {
                    // deal damage 
                    if (Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange
                        && (controller.IsFacingPlayer())
                        && !player.IsJumping())
                    {
                        player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform);
                        dealDamageAlready = true;
                    }
                }

                // left dark tracks on the ground
                if (Mathf.Abs(transform.position.x - lastFramePos) >= darkTrackDistance)
                {
                    lastFramePos = Mathf.MoveTowards(lastFramePos, transform.position.x, darkTrackDistance);

                    GameObject track = controller.SpawnSpecialEffect(trackEffect, new Vector2(lastFramePos, darkpactLocationY), false);
                    tracks.Add(track);
                    controller.GetGameManager().RegisterExtraStuff(track);
                }
                
                controller.CreateAfterImage(0.15f);
            }
            
        }

        // dash toward front
        if (dealDamageCnt > GetAttackDealDamageDelay(attackType) && !attackDashAlready)
        {
            float dashRange = GetDashRange(attackType);
            if (dashRange > 0)
            {
                transform.DOMoveX(dashRange * direction, 0.1f);
            }
            attackDashAlready = true;
            AudioManager.Instance.PlaySFX("SwordSwing", 1.5f);
        }

        if (statusTimer == 0.0f)
        {
            InitStatus(Status.Idle);

            if (attackType == AttackType.Sweep)
            {
                // after attack follow up
                endpoint = transform.position.x;
                
                while (Mathf.Abs(lastFramePos - endpoint) >= darkTrackDistance)
                {
                    lastFramePos = Mathf.MoveTowards(lastFramePos, endpoint, darkTrackDistance);
                }

                List<GameObject> tmp = new List<GameObject>(tracks);
                tracks.Clear();

                StartCoroutine(AfterBurstAttack(tmp));
            }
        }
    }

    IEnumerator AfterBurstAttack(List<GameObject> trackList)
    {
        bool alreadyDealDamage = false;

        for (int i = 0; i < trackList.Count; i++)
        {
            controller.SpawnSpecialEffect(sweepEffect, new Vector2(trackList[i].transform.position.x, darkpactLocationY), false);
            trackList[i].GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0); // hide track
            AudioManager.Instance.PlaySFX("DarkPact", 0.75f);

            // deal damage
            if (Mathf.Abs(player.transform.position.x - trackList[i].transform.position.x) < darkTrackDistance
                   && Mathf.Abs(player.transform.position.y - trackList[i].transform.position.y) < attackRange
                   && !alreadyDealDamage)
            {
                alreadyDealDamage = true;
                player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform);
            }

            // stop this immediately if this enemy is dead
            if (controller.GetCurrentStatus() == Status.Dying)
            {
                foreach (var track in trackList)
                {
                    Destroy(track);
                }

                yield break;
            }
            else
            {
                yield return new WaitForSeconds(0.05f);
            }
        }

        foreach (var track in trackList)
        {
            Destroy(track);
        }
    }

    private void DecideAttackType()
    {
        // decide attack type by distance from player
        float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
        if (distance < attackRange)
        {
            attackType = AttackType.Slam;
        }
        else
        {
            attackType = AttackType.Sweep;
        }
    }

    private string GetAttackAnimationName(AttackType attackType)
    {
        return enemyName + "Attack" + ((int)attackType + 1).ToString();
    }

    private float GetAttackDealDamageDelay(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Slam:
                return dealDamageDelayAttack1;
            case AttackType.Sweep:
                return dealDamageDelayAttack2;
            default:
                break;
        }

        return 0.0f;
    }
    private float GetDashRange(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Slam:
                return dashRangeAttack1;
            case AttackType.Sweep:
                return dashRangeAttack2;
            default:
                break;
        }

        return 0.0f;
    }

    private bool IsOnGround()
    {
       
        return transform.position.y < -2.0f;
    }
}
