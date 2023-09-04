using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ChadSlimeAI : EnemyAI
{
    [Header("Setting")]
    [SerializeField] private bool haveTurnAnimation;
    [SerializeField] private int attackDamageBase;
    [SerializeField] private int attackDamageMax;
    [SerializeField] private bool allowAttack;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float staminaRegenInterval;
    [SerializeField] private int initialStamina = 50;

    private EnemyControl controller;

    [Header("References")]
    private Animator animator;
    private LayerMask playerLayer;
    private SpriteRenderer graphic;
    private string enemyName;
    private Controller player;
    private GameObject impactParticleEffect;
    private GameObject jumpDustEffect;
    private GameObject waterSplashEffect;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    
    bool isJumping;
    bool isFall;
    float afterImgInterval = 0.2f;
    float afterImgCnt;
    float staminaRegenCnt;

    private void Awake()
    {
        impactParticleEffect = Resources.Load("Prefabs/ImpactGround") as GameObject;
        jumpDustEffect = Resources.Load("Prefabs/JumpDust") as GameObject;
        waterSplashEffect = Resources.Load("Prefabs/WaterSplash") as GameObject;
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

        SetScalingRule(controller.GetLevel());

        controller.RegenStamina(initialStamina);
    }

    private void SetScalingRule(int level)
    {
        attackDamageBase += level / 4;
        moveSpeed += 0.1f * (float)level + Random.Range(-0.5f, 0.5f);

        controller.AddMaxHp(level * 4);

        if (level > 10)
        {
            controller.AddMaxHp(60);
            attackDamageBase += Random.Range(1,3);
        }
    }

    private void FixedUpdate()
    {
        if (controller.IsPaused()) return;  // ÉQÅ[ÉÄí‚é~íÜÇÕêßå‰ÇµÇ»Ç¢

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
                AttackingCtrl();
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
                isJumping = false;
                isFall = false;

                animator.Play(enemyName + "Jump");
                statusTimer = controller.FindAnimation(animator, enemyName + "Jump").length - Time.fixedDeltaTime;
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                break;
            case Status.Turning:
                animator.Play(enemyName + "Turn");
                statusTimer = controller.FindAnimation(animator, enemyName + "Turn").length - Time.fixedDeltaTime;
                break;
            case Status.Chasing:
                break;
            case Status.Fleeing:
                break;
            case Status.SpecialAbility:
                break;
            case Status.Dying:
                AudioManager.Instance.PlaySFX("JellySlimeDead");
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

        // JUMP
        if (statusTimer == 0.0f && controller.IsStaminaMax() && controller.GetRigidBody().velocity.y == 0.0f && allowAttack && player.IsAlive())
        {
            controller.UseAllStamina();
            InitStatus(Status.Attacking);
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

    void AttackingCtrl()
    {
        // Trying to jump
        if (!isJumping)
        {
            if (statusTimer < controller.FindAnimation(animator, enemyName + "Jump").length * 0.8f)
            {
                isJumping = true;
                controller.GetRigidBody().AddForce(new Vector2(0f, 900f));
                controller.SetImmumetoKnockback(true);
                AudioManager.Instance.PlaySFX(enemyName + "Jump", 0.75f);

                // FX
                Transform tmp = Instantiate(jumpDustEffect, new Vector2(transform.position.x, -2.619f), Quaternion.identity).transform;
                tmp.transform.DOScale(2.0f, 0.0f);
            }
        }
        else if (!isFall)
        {
            // IF SLIME IS IN THE AIR IT CAN TURN INSTANTLY
            if ((player.transform.position.x > controller.transform.position.x
                && graphic.flipX)
                ||
                (player.transform.position.x < controller.transform.position.x
                && !graphic.flipX))
            {
                graphic.flipX = !graphic.flipX;
                return;
            }

            // MOVE
            if (transform.position.y > -1f)
            {
                if (Mathf.Abs(player.transform.position.x - transform.position.x) > moveSpeed * Time.deltaTime)
                {
                    int direction = graphic.flipX ? -1 : 1;
                    transform.position = new Vector2(transform.position.x + direction * moveSpeed * Time.deltaTime, transform.position.y);
                }
            }

            // AFTER IMAGE
            afterImgCnt -= Time.deltaTime;
            if (afterImgCnt < 0.0f)
            {
                afterImgCnt = afterImgInterval;
                controller.CreateAfterImage();
            }

            // FALL
            if (statusTimer < controller.FindAnimation(animator, enemyName + "Jump").length * 0.4f)
            {
                isFall = true;
                isJumping = true;
            }
        }
        else if (isFall)
        {
            // FALL
            transform.position = new Vector2(transform.position.x, transform.position.y - (10f * Time.deltaTime));

            // AFTER IMAGE
            afterImgCnt -= Time.deltaTime;

            // LAND
            if (transform.position.y < -1.0f)
            {
                controller.SetImmumetoKnockback(false);
                transform.position = new Vector2(transform.position.x, -2.369751f);
                InitStatus(Status.Idle);
                AudioManager.Instance.PlaySFX("impact", 0.75f);

                controller.GetGameManager().ScreenImpactGround(0.04f, 0.4f);

                GameObject tmp = Instantiate(waterSplashEffect, new Vector2(transform.position.x, -3.075f), Quaternion.identity);
                tmp.transform.DOScale(1.5f, 0.0f);
                tmp = Instantiate(impactParticleEffect, Vector2.Lerp(transform.position, new Vector2(transform.position.x, transform.position.y - controller.GetCollider().bounds.size.y / 2f), 0.5f), Quaternion.identity);
                tmp.GetComponent<ParticleScript>().SetParticleColor(controller.GetGameManager().GetThemeColor());

                // DEAL DAMAGE
                if (Mathf.Abs(player.transform.position.x - transform.position.x) < controller.GetCollider().bounds.size.x / 2f)
                {
                    player.StartJump(false, true);
                    player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform);
                }
            }
        }
    }
}
