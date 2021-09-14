using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SkellyAI : MonoBehaviour
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
        attackDamageBase += level * 2;
        attackDamageMax += level * (1 / 5);
        moveSpeed += Random.Range(-0.5f, 0.5f); ;
        staminaRegenInterval = staminaRegenInterval / ((float)level/2);

        controller.AddMaxHp(level * 5);

        if (level > 10)
        {
            controller.AddMaxHp(25);
            attackDamageBase += Random.Range(1, 3);
        }
        if (level > 20)
        {
            controller.AddMaxHp(35);
            attackDamageBase += Random.Range(1, 3);
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
                AudioManager.Instance.PlaySFX("heartbeat");
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
        int direction = graphic.flipX ? -1 : 1;
        if (controller.IsStaminaMax() && allowAttack && player.IsAlive()
            && Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * attackRange / 2f))) < attackRange
            && GetComponent<Rigidbody2D>().velocity.y == 0.0f)
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
            float direction = graphic.flipX ? -1 : 1;
            if (Mathf.Abs(player.transform.position.x -  (transform.position.x + (direction * attackRange / 2f))) < attackRange
                && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange/2f
                && ((player.transform.position.x > transform.position.x && !graphic.flipX) || (player.transform.position.x < transform.position.x && graphic.flipX)))
            {
                player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax+1), transform);
            }
            AudioManager.Instance.PlaySFX("wind");
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
}
