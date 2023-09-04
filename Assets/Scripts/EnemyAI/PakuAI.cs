using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PakuAI : EnemyAI
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
    [SerializeField] private float defaultFlyHeight = 1.5f;

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
    float afterImgInterval = 0.05f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;
    bool startedCharge;

    float targetMoveX;
    float flightHeight;

    private GameObject chargeEffect;
    private GameObject lightningBall;
    private GameObject lightningSparkEffect;

    private List<LightingBall> lightningBallList;

    private void Awake()
    {
        chargeEffect = Resources.Load("Prefabs/ElectricSpark") as GameObject;
        lightningBall = Resources.Load("Prefabs/LightningBall") as GameObject;
        lightningSparkEffect = Resources.Load("Prefabs/LightningSpark") as GameObject;
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

        lightningBallList = new List<LightingBall>();
    }

    private void SetScalingRule(int level)
    {
        attackDamageBase += (int)((float)level * 1.5f);
        attackDamageMax += level / 5;
        moveSpeed += Random.Range(-moveSpeed/2f, moveSpeed/2f);

        controller.AddMaxHp(level * 8);
    }

    private void FixedUpdate()
    {
        if (controller.IsPaused()) return;  // ƒQ[ƒ€’âŽ~’†‚Í§Œä‚µ‚È‚¢
        CheckFlags();

        statusTimer = Mathf.Max(statusTimer - Time.deltaTime, 0.0f);
        flightHeight += Time.deltaTime;
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
                RegenerateStamina();
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
                controller.RegenStamina(5);
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
                AudioManager.Instance.PlaySFX(enemyName + "Dead", 0.5f);
                controller.GetRigidBody().bodyType = RigidbodyType2D.Dynamic;
                foreach (LightingBall ball in lightningBallList)
                {
                    ball.DestroyWithOwner();
                }
                lightningBallList.Clear();
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


        // KEEP FLYING
        float flightHeightSin = Mathf.Sin(flightHeight);
        transform.position = new Vector2(transform.position.x, defaultFlyHeight + flightHeightSin * 1.0f);

        // CHANGE STATUS
        if (statusTimer == 0.0f && player.IsAlive())
        {
            InitStatus(Status.Chasing);
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
        // calculate flying height
        float flightHeightSin = Mathf.Sin(flightHeight);

        // MOVE POSITION
        int direction = graphic.flipX ? -1 : 1;
        transform.DOMove(new Vector2(transform.position.x + moveSpeed * direction * Time.deltaTime, defaultFlyHeight + flightHeightSin * 1.0f), 0.1f);

        // Turn around if reach the edge of the map
        if (transform.position.x < -8.8f)
        {
            graphic.flipX = false;
        }
        if (transform.position.x > 8.8f)
        {
            graphic.flipX = true;
        }

        if (controller.IsStaminaMax() && Mathf.Abs(transform.position.x) < 6.5f)
        {
            SpawnSpecialEffect();
            controller.UseAllStamina();
            controller.RegenStamina(Random.Range(0, 40));
        }
    }

    void FleeingCtrl()
    {
       
    }

    float CalculateTargetMoveX()
    {
        int fleedirection = (transform.position.x > player.transform.position.x) ? 1 : -1;

        return player.transform.position.x + (fleedirection * (attackRange * 2.5f));
    }

    void AttackCtrl()
    {
        
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
        // SE
        AudioManager.Instance.PlaySFX("PakuSpawnBall", 0.5f);

        // SPARK
        GameObject tmp = Instantiate(lightningSparkEffect, transform.position, Quaternion.identity);
        tmp.transform.SetParent(transform.parent);

        // BALL
        tmp = Instantiate(lightningBall, transform.position, Quaternion.identity);
        tmp.transform.SetParent(transform.parent);
        tmp.GetComponent<LightingBall>().Initialize(controller.GetGameManager(), controller.GetPlayer(), this, Random.Range(1, 4), 2f, attackDamageBase, attackDamageMax);

        // ADD TO LIST
        lightningBallList.Add(tmp.GetComponent<LightingBall>());
    }

    public void ElectricBallUnRegister(LightingBall ball)
    {
        lightningBallList.Remove(ball);
    }
}
