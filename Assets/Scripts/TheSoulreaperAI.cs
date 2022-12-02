using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TheSoulreaperAI : MonoBehaviour
{
    enum AttackType
    {
        Cast,
        BackstabAttack,
        Heal,
        Min = 0,
        Max = Heal + 1,
    }

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
    [SerializeField] GameObject flameEffect;
    [SerializeField] GameObject electricChargeEffect;
    [SerializeField] GameObject lightningBall;
    [SerializeField] GameObject jumpDustEffect;
    [SerializeField] GameObject thunderStrikeEffect;
    [SerializeField] GameObject thunderRayEffect;
    [SerializeField] GameObject eternalFlameEffect;
    [SerializeField] GameObject illusionOrigin;

    [Header("Special Setting")]
    [SerializeField] private float attackChargeEffectInterval;
    [SerializeField] private float jumpTime = 2.0f;
    [SerializeField] private int jumpNumber;
    [SerializeField] private float afterImgInterval = 0.2f;
    [SerializeField] private Color illusionColor = new Color(0.0f, 0.0f, 0.5f, 0.9f);
    [SerializeField] private bool isIllusion = false;

    [Header("Debug")]
    [SerializeField] private float afterImgCnt;
    [SerializeField] private float statusTimer;
    [SerializeField] private AttackType currentAttackType;
    [SerializeField] private bool isFalling;

    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;
    float attackChargeEffectCount;
    float jumpTimeCount;
    float footstepCount = 0.0f;

    Coroutine eternalFlameCoroutine;
    Coroutine backstabCoroutine;
    bool isBackstabRunning;
    float shockwaveHitPlayerCd;


    private EnemyControl illusionCopy;
    bool isInitialized = false;

    // Update is called once per frame
    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
        controller = GetComponent<EnemyControl>();
        animator = controller.GetAnimator();
        playerLayer = controller.GetPlayerLayer();
        graphic = controller.GetGraphic();
        enemyName = controller.GetName();
        statusTimer = 0.0f;

        player = FindObjectOfType<Controller>();

        controller.RegenStamina(initialStamina);

        if (isIllusion)
        {
            graphic.color = illusionColor;
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
                // Jumping
                JumpCtrl();
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
                InitAttack();
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length;
                break;
            case Status.Turning:
                if (haveTurnAnimation)
                {
                    animator.Play(enemyName + "Idle");
                    statusTimer = controller.FindAnimation(animator, enemyName + "Idle").length - Time.fixedDeltaTime;
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
                animator.Play(enemyName + "Move");
                animator.SetBool("Move", true);
                footstepCount = 0.0f;
                break;
            case Status.Fleeing:
                break;
            case Status.SpecialAbility:
                break;
            case Status.Dying:
                // SE
                AudioManager.Instance.PlaySFX(enemyName + "Dead");
                break;
            default:
                InitStatus(Status.Idle);
                break;
        }
    }

    private void Update()
    {
        if (!ReferenceEquals(illusionCopy, null) && !isIllusion && illusionCopy.IsAlive())
        {
            controller.SetArmor(1000);
        }
        else
        {
            controller.SetArmor(0);
        }
    }

    void InitAttack()
    {
        DecideNextAttackType();

        switch (currentAttackType)
        {
            case AttackType.BackstabAttack:
            case AttackType.Heal:
                animator.Play(enemyName + "Attack");
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length;
                break;

            case AttackType.Cast:
                animator.Play(enemyName + "Cast");
                statusTimer = controller.FindAnimation(animator, enemyName + "Cast").length;
                break;
            default:
                break;
        }

        attackChargeEffectCount = 0.0f;
        dealDamageCnt = 0.0f;
        dealDamageAlready = false;
        controller.UseAllStamina();
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
        if (currentAttackType == AttackType.Cast)
        {
            CastIllusionCtrl();
            return;
        }

        dealDamageCnt += Time.deltaTime;
        
        if (dealDamageCnt > dealDamageDelay && !dealDamageAlready)
        {
            if (player.IsDashing() || isBackstabRunning)
            {
                if (!isBackstabRunning && !controller.IsFacingPlayer())
                {
                    backstabCoroutine = StartCoroutine(BackStab());
                }

                // pause until player done dashing
                animator.speed = 0.0f;
                dealDamageCnt -= Time.deltaTime;
                return;
            }
            else if (animator.speed == 0.0f)
            {
                // go back
                animator.speed = 1.0f;
            }

            // deal damage 
            float direction = (float)controller.GetDirectionInteger();
            if (Mathf.Abs(player.transform.position.x -  transform.position.x) < attackRange + 1.5f
                && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange/2f
                && controller.IsFacingPlayer())
            {
                int calculateDamage = attackDamageBase + Random.Range(0, attackDamageMax + 1);
                if (player.DealDamage(calculateDamage, transform))
                {
                    // heal cultist
                    if (!isIllusion) controller.Heal(50 + calculateDamage);
                    
                    //  cancel previous buff
                    if (!ReferenceEquals(eternalFlameCoroutine, null))
                    {
                        StopCoroutine(eternalFlameCoroutine);
                    }

                    // Apply buff into player
                    float buffDuration = 4f;
                    eternalFlameCoroutine = StartCoroutine(EternalFlameBuff(buffDuration, 0.15f));
                    player.DisableDash(buffDuration);
                }
            }
            else
            {
                // didn't hit player -> create a shockwave
                shockwaveHitPlayerCd = 0.0f;
                StartCoroutine(Shockwave(-1.65f,
                    transform.position.x + (2.0f * (float)controller.GetDirectionInteger()), 
                    0.1f, (float)controller.GetDirectionInteger() * 0.6f, 1));
            }

            // create a circle of effects around it
            int numberOfEffects = 10;
            for (int i = 0; i < numberOfEffects; i++)
            {
                float radius = 1.5f;
                float progress = ((float)i / (float)numberOfEffects);
                float currentRadian = progress * 2.5f * Mathf.PI;
                float xScaled = Mathf.Cos(currentRadian);
                float yScaled = Mathf.Sin(currentRadian);
                Vector2 pos = new Vector2(transform.position.x + (direction * (attackRange / 2.0f)), transform.position.y)
                            + new Vector2(xScaled * radius, yScaled * radius);
                if ((xScaled >= 0 && direction > 0) || (xScaled <= 0 && direction < 0))
                {
                    var obj = controller.SpawnSpecialEffect(flameEffect, pos, false);
                    if (isIllusion) obj.GetComponent<SpriteRenderer>().color = illusionColor;
                }
            }

            AudioManager.Instance.PlaySFX("wind");
            dealDamageAlready = true;
        }

        if (statusTimer == 0.0f)
        {
            InitStatus(Status.Idle);
        }
    }

    IEnumerator EternalFlameBuff(float duration, float effectInterval)
    {
        Vector3 lastPos = player.transform.position;
        float timeElapsed = 0.0f;
        while (timeElapsed < duration && player.IsAlive())
        {
            // deal damage
            if (player.DealDamage(1, transform))
            {
                // make player jump
                if (player.transform.position.y < 2.5f)
                {
                    player.ForceJump(Random.Range(0.2f, 0.6f));
                }

                // create an effect in the center
                lastPos = player.transform.position;
                GameObject obj = controller.SpawnSpecialEffect(eternalFlameEffect, lastPos, false);
                if (isIllusion) obj.GetComponent<SpriteRenderer>().color = illusionColor;

                if (!isIllusion) controller.Heal(5);

                // count time
                timeElapsed += effectInterval;
                yield return new WaitForSeconds(effectInterval);
            }
            else
            {
                // create an effect in last position, end this buff
                GameObject obj = controller.SpawnSpecialEffect(eternalFlameEffect, lastPos, false);
                if (isIllusion) obj.GetComponent<SpriteRenderer>().color = illusionColor;
                timeElapsed = duration;
                shockwaveHitPlayerCd = 0.0f; // player can be hit by shockwave again
            }
        }
    }
    
    private void DecideNextAttackType()
    {
        currentAttackType = currentAttackType + 1;

        if (currentAttackType >= AttackType.Max)
        {
            currentAttackType = AttackType.Min;
        }

        if (currentAttackType == AttackType.Cast && (controller.GetCurrentHPPercentage() > 0.6f || isIllusion || (!ReferenceEquals(illusionCopy, null) && illusionCopy.IsAlive())))
        {
            currentAttackType = currentAttackType + 1; // skip
        }
    }

    public void StartJump()
    {
        // FX
        if (transform.position.y < -1f)
        {
            Transform tmp = Instantiate(jumpDustEffect, new Vector2(transform.position.x, -2.619f), Quaternion.identity).transform;
            tmp.SetParent(transform.parent);
            tmp.transform.DOScale(2.0f, 0.0f);
        }

        // velocity
        Debug.Log(gameObject.name + "is trying to jump!");
        controller.GetRigidBody().velocity = new Vector2(0.0f, 10.0f);
        controller.GetRigidBody().AddForce(new Vector2(0.0f, 60f));

        // animator
        animator.SetBool("Fall", false);
        animator.SetBool("Jump", true);
        animator.Play(enemyName + "Jump");

        // SE
        AudioManager.Instance.PlaySFX("jump", 2.0f);
        controller.GetGameManager().ScreenImpactGround(0.02f, 0.02f);

        // flag
        isFalling = false;
        jumpTimeCount = 0.0f;
        afterImgCnt = afterImgInterval;
        dealDamageAlready = false;
        controller.SetImmumetoKnockback(true);

        InitStatus(Status.Fleeing);
    }

    public void JumpCtrl()
    {
        // count
        jumpTimeCount += Time.deltaTime;

        if (jumpTimeCount > jumpTime && !isFalling)
        {
            // animator
            animator.SetBool("Fall", true);
            animator.SetBool("Jump", false);
            isFalling = true;
        }

        if (!isFalling)
        {
            // move forward
            transform.localPosition = new Vector2(transform.localPosition.x + ((float)controller.GetDirectionInteger() * moveSpeed * 2.0f * Time.deltaTime),
                                                  transform.localPosition.y);
        }

        else if (isFalling)
        {
            // reach the ground
            if (transform.localPosition.y < -1.7f)
            {
                isFalling = false;
                controller.GetRigidBody().isKinematic = false;

                // animator
                animator.SetBool("Fall", false);

                // snap character into the ground
                transform.localPosition = new Vector2(transform.localPosition.x, -1.842454f);

                // flag reset
                statusTimer = 0.0f;
                dealDamageCnt = 0.0f;
                dealDamageAlready = false;
                controller.UseAllStamina();
                controller.SetImmumetoKnockback(false);

                // change status
                InitStatus(Status.Idle);
            }
        }
    }

    private void CastIllusionCtrl()
    {
        if (statusTimer == 0.0f)
        {
            InitStatus(Status.Idle);

            // copy 
            illusionCopy = Instantiate(illusionOrigin, transform.position, Quaternion.identity).GetComponent<EnemyControl>();
            illusionCopy.transform.SetParent(transform.parent, true);
            illusionCopy.GetGraphic().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            controller.GetGameManager().RegisterMonsterInList(illusionCopy);
            illusionCopy.SetLevel(illusionCopy.GetLevel());

            illusionCopy.Initialization(); // force initialize
            illusionCopy.GetComponent<TheSoulreaperAI>().Initialize(); // force initialize

            var illusionGraphic = illusionCopy.GetComponent<EnemyControl>();

            if (!ReferenceEquals(illusionGraphic, null))
            {
                Debug.Log("illusionGraphic is not null");
                illusionGraphic.GetGraphic().flipX = graphic.flipX;
            }

            illusionCopy.gameObject.name = controller.GetName() + " (Illusion)";

            illusionCopy.GetComponent<TheSoulreaperAI>().StartJump();
        }
    }
    
    IEnumerator BackStab()
    {
        graphic.DOColor(new Color(0.0f, 0.0f, 0.0f, 0.0f), 0.5f);
        isBackstabRunning = true;
        controller.SetInvulnerable(true);

        yield return new WaitForSeconds(0.6f);

        controller.TurnTowardPlayer();
        if (isIllusion)
        {
            graphic.DOColor(illusionColor, 0.3f);
        }
        else
        {
            graphic.DOColor(new Color(1, 1, 1, 1), 0.3f);
        }

        yield return new WaitForSeconds(0.4f);
        isBackstabRunning = false;
        controller.SetInvulnerable(false);

    }

    IEnumerator Shockwave(float y, float initialPosition, float timeinterval, float rangeinterval, int bounceNumber)
    {
        // WAIT
        yield return new WaitForSeconds(timeinterval);
        shockwaveHitPlayerCd = Mathf.Max(shockwaveHitPlayerCd- timeinterval, 0.0f);

        // PLAY SPECIAL EFFECT
        Transform transf = Instantiate(flameEffect, new Vector2(initialPosition, y), Quaternion.identity).transform;
        Destroy(transf.gameObject, timeinterval * 10f);
        if (isIllusion)
        {
            transf.GetComponent<SpriteRenderer>().color = illusionColor;
            transf.GetComponent<SpriteRenderer>().DOColor(new Color(illusionColor.r, illusionColor.g, illusionColor.b, 0.75f), timeinterval * 10f);
        }
        else
        {
            transf.GetComponent<SpriteRenderer>().DOFade(0.0f, timeinterval * 10f);
        }
        
        // PLAY SOUND
        AudioManager.Instance.PlaySFX("burst", 0.10f);

        // DEAL DAMAGE
        if (Mathf.Abs(player.transform.position.x - initialPosition) < 1.2f
            && Mathf.Abs(player.transform.position.y - y) < 1.4f && shockwaveHitPlayerCd == 0.0f)
        {
            shockwaveHitPlayerCd = 1.5f;
            player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transf);

            // Apply buff into player
            float buffDuration = 4f;
            eternalFlameCoroutine = StartCoroutine(EternalFlameBuff(buffDuration, 0.15f));
            player.DisableDash(buffDuration);
        }

        // LOOP
        if (initialPosition < -9f || initialPosition > 9f)
        {
            if (bounceNumber > 0)
            {
                StartCoroutine(Shockwave(y, initialPosition - rangeinterval, timeinterval * 2f, -rangeinterval, bounceNumber - 1));
            }
        }
        else
        {
            StartCoroutine(Shockwave(y, initialPosition + rangeinterval, timeinterval, rangeinterval, bounceNumber));
        }
    }

    public EnemyControl GetController()
    {
        return controller;
    }
}
