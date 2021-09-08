using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ShaddoAI : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private bool haveTurnAnimation;
    [SerializeField] private int attackDamageBase;
    [SerializeField] private int attackDamageMax;
    [SerializeField] private bool allowAttack = true;
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
    GameObject projectilePrefab;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    [SerializeField] int attackCnt;

    bool isDashing;
    float afterImgInterval = 0.2f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    float targetMoveX;
    bool shootedProjectile;
    private void Awake()
    {
        projectilePrefab = Resources.Load("Prefabs/ShaddoProjectile") as GameObject;
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
    }

    private void SetScalingRule(int level)
    {
        attackDamageBase += level * 2;
        attackDamageMax += level * (1 / 5);
        moveSpeed += 0.05f * (float)level;
        staminaRegenInterval = staminaRegenInterval / ((float)level/2);

        controller.AddMaxHp(level * 2);
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
                break;
            case Status.Fleeing:
                FleeingCtrl();
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
                animator.Play(enemyName + "Idle");
                statusTimer = 0.5f; // a delay before the next action
                break;
            case Status.Attacking:
                AudioManager.Instance.PlaySFX(enemyName + "Shoot");
                AudioManager.Instance.PlaySFX(enemyName + "ShootTwo");
                animator.Play(enemyName + "Attack");
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length * 2f;
                dealDamageCnt = 0.0f;
                attackCnt = 0;
                controller.RegenStamina(100);
                shootedProjectile = false;
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
                    animator.Play(enemyName + "Idle");
                    statusTimer = turningTime;
                }
                break;
            case Status.Chasing:
                isDashing = true;
                animator.Play(enemyName + "Move");
                break;
            case Status.Fleeing:
                AudioManager.Instance.PlaySFX(enemyName + "Move", 0.4f);
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
        if (statusTimer > 0.0f) return;

        // decide where to go
        targetMoveX = transform.position.x >= 0.0f ? -6.09f : 6.09f;

        graphic.flipX = (transform.position.x > targetMoveX);

        // MOVE
        int direction = graphic.flipX ? -1 : 1;
        if (player.IsAlive() && allowAttack)
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

    void FleeingCtrl()
    {
        float direction = graphic.flipX ? -1 : 1;

        // move to target position
        transform.DOMoveX(transform.position.x + (direction * moveSpeed * Time.deltaTime), 0.0f, false);

        // check if reach destination
        if (Mathf.Abs(transform.position.x - targetMoveX) < moveSpeed * Time.deltaTime)
        {
            transform.position = new Vector2(targetMoveX, transform.position.y);

            // change new stats
            animator.SetBool("Move", false);
            InitStatus(Status.Attacking);
            return;
        }
    }

    void AttackCtrl()
    {
        graphic.flipX = (transform.position.x > 0);

        dealDamageCnt += Time.deltaTime;
        if (dealDamageCnt > dealDamageDelay && !shootedProjectile)
        {
            // reset
            dealDamageCnt = 0.0f;
            attackCnt++;
            shootedProjectile = true;

            // spawn projectile
            float direction = graphic.flipX ? -1 : 1;
            GameObject proj = Instantiate(projectilePrefab, new Vector2(transform.position.x + (direction * GetComponent<Collider2D>().bounds.size.x / 2f), transform.position.y), Quaternion.identity);
            proj.GetComponent<projectile>().Initialize(new Vector2(direction, 0.0f), 10f, attackDamageBase + Random.Range(0, attackDamageMax + 1), player, 1f);
        }

        if (statusTimer <= 0.0f)
        {
            // change status if shoot more than two projectile
            if (attackCnt >= 2)
            {
                InitStatus(Status.Idle);
            }
            else
            {
                // attack again
                AudioManager.Instance.PlaySFX(enemyName + "Shoot");
                AudioManager.Instance.PlaySFX(enemyName + "ShootTwo");
                animator.Play(enemyName + "Attack");
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length * 2f;
                dealDamageCnt = 0.0f;
                shootedProjectile = false;
            }
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
}
