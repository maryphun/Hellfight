using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TheHungryOneAI : EnemyAI
{
    enum AttackType
    {
        SpawnLightningBall,
        NormalAttack,
        JumpSlam,
        Min = 0,
        Max = JumpSlam+1,
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
    [SerializeField] GameObject electricShockEffect;
    [SerializeField] GameObject electricChargeEffect;
    [SerializeField] GameObject lightningBall;
    [SerializeField] GameObject jumpDustEffect;
    [SerializeField] GameObject thunderStrikeEffect;
    [SerializeField] GameObject thunderRayEffect;
    [SerializeField] GameObject thunderExplosionEffect;

    [Header("Special Setting")]
    [SerializeField] private float attackChargeEffectInterval;
    [SerializeField] private List<float> jumpTime;
    [SerializeField] private int jumpNumber;
    [SerializeField] private float afterImgInterval = 0.2f;
    [SerializeField] private float footstepSEInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private float afterImgCnt;
    [SerializeField] private int jumpCount = 0;
    [SerializeField] private float statusTimer;
    [SerializeField] private AttackType currentAttackType;
    
    bool isFalling;
    float staminaRegenCnt;
    float dealDamageCnt;
    bool dealDamageAlready;
    float attackChargeEffectCount;
    float jumpTimeCount;
    float footstepCount = 0.0f;

    Coroutine ElectricBuffCoroutine;
    private List<LightingBall> lightningBallList;

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
        lightningBallList = new List<LightingBall>();
        
        // SE
        AudioManager.Instance.PlaySFX("TheHungryOneGrowl", 0.75f);
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
                AudioManager.Instance.PlaySFX("TheHungryOneLaugh", 1f);
                AudioManager.Instance.PlaySFX(enemyName + "Dead");
                break;
            default:
                InitStatus(Status.Idle);
                break;
        }
    }

    void InitAttack()
    {
        DecideNextAttackType();

        switch (currentAttackType)
        {
            case AttackType.NormalAttack:
                animator.Play(enemyName + "Attack");
                statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length;
                break;
            case AttackType.SpawnLightningBall:
                AudioManager.Instance.PlaySFX("Magic Cast 01", 1.0f);
                float posX = Mathf.MoveTowards(player.transform.localPosition.x, -100f, 4f);
                if (posX > -8.1f) SpawnLightningBall(new Vector2(posX, 5.7f));
                
                posX = player.transform.localPosition.x;
                SpawnLightningBall(new Vector2(posX, 5.7f));

                posX = Mathf.MoveTowards(player.transform.localPosition.x, 100f, 4f);
                if (posX < 8.1f) SpawnLightningBall(new Vector2(posX, 5.7f));

                InitStatus(Status.Idle);
                break;
            case AttackType.JumpSlam:
                jumpCount = 0;
                StartJump();
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

        // SE
        footstepCount += Time.deltaTime;
        if (footstepCount >= footstepSEInterval)
        {
            footstepCount = 0.0f;
            AudioManager.Instance.PlaySFX("TheHungryOneFootstep", 1.5f);
            controller.GetGameManager().ScreenImpactGround(0.02f, 0.02f);
        }

        // reach destination
        if (Mathf.Abs(player.transform.position.x- transform.position.x ) < attackRange)
        {
            InitStatus(Status.Attacking);
        }
    }

    void AttackCtrl()
    {
        if (currentAttackType == AttackType.JumpSlam)
        {
            JumpSlamCtrl();
            return;
        }
        dealDamageCnt += Time.deltaTime;

        // charge effect
        if (!dealDamageAlready)
        {
            attackChargeEffectCount += Time.deltaTime;
            if (attackChargeEffectCount > attackChargeEffectInterval)
            {
                // count
                attackChargeEffectCount = 0.0f;

                // SE
                AudioManager.Instance.PlaySFX("Magic Buff 01", 0.25f);

                // effect
                Vector2 radius = new Vector2(1.0f,2.0f);
                float reversedDirection = -(float)controller.GetDirectionInteger();
                float xScaled = Mathf.Cos(statusTimer + Random.Range(0.0f, 1.0f));
                float yScaled = Mathf.Sin(statusTimer + Random.Range(0.0f, 1.0f));
                Vector2 pos = new Vector2(transform.position.x + (reversedDirection * 1.5f), transform.position.y + 1f)
                            + new Vector2(xScaled * radius.x, yScaled * radius.y);
                controller.SpawnSpecialEffect(electricChargeEffect, pos, false);
            }
        }

        if (dealDamageCnt > dealDamageDelay && !dealDamageAlready)
        {
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
                    controller.Heal(50 + calculateDamage);

                    //  cancel previous buff
                    if (!ReferenceEquals(ElectricBuffCoroutine, null))
                    {
                        StopCoroutine(ElectricBuffCoroutine);
                    }

                    // Apply buff into player
                    float buffDuration = 2.5f;
                    ElectricBuffCoroutine = StartCoroutine(ElectricShockBuff(buffDuration, 0.45f));
                    player.DisableDash(buffDuration);
                }
            }

            // SE
            AudioManager.Instance.PlaySFX("thunder", 1.5f);

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
                    controller.SpawnSpecialEffect(electricShockEffect, pos, false);
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

    IEnumerator ElectricShockBuff(float duration, float effectInterval)
    {
        float timeElapsed = 0.0f;
        while (timeElapsed < duration && player.IsAlive())
        {
            // create an effect in the center
            GameObject obj = controller.SpawnSpecialEffect(electricShockEffect, player.transform.position, false);
            obj.transform.SetParent(player.transform);

            // suck all player's stamina
            controller.GetGameManager().ShakeStaminaBar(4);
            player.DamagePercentage(0.0f, 1.0f);
            player.StopStaminaRegenerate();

            // SE
            AudioManager.Instance.PlaySFX("thunder", 0.55f);

            // count time
            timeElapsed += effectInterval;
            yield return new WaitForSeconds(effectInterval);
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

    private void DecideNextAttackType()
    {
        currentAttackType = currentAttackType + 1;

        if (currentAttackType >= AttackType.Max)
        {
            currentAttackType = AttackType.Min;
        }
    }

    void SpawnLightningBall(Vector2 pos)
    {
        // BALL
        GameObject tmp = Instantiate(lightningBall, pos, Quaternion.identity);
        tmp.transform.SetParent(transform.parent);
        tmp.GetComponent<LightingBall>().Initialize(controller.GetGameManager(), controller.GetPlayer(), this, 1, 2f, attackDamageBase, attackDamageMax);
        
        // ADD TO LIST
        lightningBallList.Add(tmp.GetComponent<LightingBall>());
    }

    public void ElectricBallUnRegister(LightingBall ball)
    {
        lightningBallList.Remove(ball);
    }

    private void StartJump()
    {
        // FX
        if (transform.position.y < -1f)
        {
            Transform tmp = Instantiate(jumpDustEffect, new Vector2(transform.position.x, -2.619f), Quaternion.identity).transform;
            tmp.SetParent(transform.parent);
            tmp.transform.DOScale(2.0f, 0.0f);
        }

        // velocity
        controller.GetRigidBody().velocity = new Vector2(0.0f, 10.0f);
        controller.GetRigidBody().AddForce(new Vector2(0.0f, 60f));

        // animator
        animator.SetBool("Fall", false);
        animator.SetBool("Jump", true);
        animator.Play(enemyName + "Jump");

        // SE
        AudioManager.Instance.PlaySFX("jump", 2.0f);
        AudioManager.Instance.PlaySFX("TheHungryOneBreath", 0.8f);
        controller.GetGameManager().ScreenImpactGround(0.02f, 0.02f);

        // flag
        isFalling = false;
        jumpTimeCount = 0.0f;
        afterImgCnt = afterImgInterval;
        dealDamageAlready = false;
        controller.SetImmumetoKnockback(true);
    }

    private void JumpSlamCtrl()
    {
        // count
        jumpTimeCount += Time.deltaTime;
        afterImgCnt -= Time.deltaTime;

        if (afterImgCnt < 0.0f)
        {
            afterImgCnt = afterImgInterval;
            controller.CreateAfterImage();
        }

        // check fall
        if (jumpTimeCount > jumpTime[jumpCount] && !isFalling)
        {
            // animator
            animator.SetBool("Fall", true);
            animator.SetBool("Jump", false);

            if (jumpCount >= jumpNumber-1)
            {
                // last jump
                controller.TurnTowardPlayer();
                animator.Play(enemyName + "Slam");
                statusTimer = controller.FindAnimation(animator, enemyName + "Slam").length - 0.5f;

                // SE
                AudioManager.Instance.PlaySFX("TheHungryOneBreath2", 1.5f);

                // VFX
                float posX = transform.position.x + ((float)controller.GetDirectionInteger() * 3f);
                controller.SpawnSpecialEffect(thunderRayEffect, new Vector2(posX, 0.86f), false);
            }
            else
            {
                // fall 
                animator.Play(enemyName + "Fall");
                statusTimer = 0.0f;

                // SE
                AudioManager.Instance.PlaySFX("TheHungryOneBreath", 1.0f);

                // VFX
                controller.SpawnSpecialEffect(thunderStrikeEffect, new Vector2(controller.transform.position.x, -1.07f), false);
            }

            // flag
            jumpTimeCount = 0.0f;
            isFalling = true;
            dealDamageAlready = false; 

            controller.GetRigidBody().isKinematic = true;
        }

        if (!isFalling)
        {
            // move forward
            transform.localPosition = new Vector2(transform.localPosition.x + ((float)controller.GetDirectionInteger() * moveSpeed * 2.0f * Time.deltaTime), 
                                                  transform.localPosition.y);
        }
        else if (isFalling)
        {
            if (statusTimer <= 0.0f)
            {
                // FALL
                transform.localPosition = new Vector2(transform.localPosition.x, transform.localPosition.y - (40.0f * Time.deltaTime));

                afterImgCnt = 0.0f;
            }

            // reach the ground
            if (transform.localPosition.y < -2.2f)
            {
                isFalling = false;
                controller.GetRigidBody().isKinematic = false;

                // animator
                animator.SetBool("Fall", false);

                // snap character into the ground
                transform.localPosition = new Vector2(transform.localPosition.x, -2.334f);

                // jump the player if it's on ground
                if (!player.IsJumping()) player.ForceJump();

                // visuals and sounds
                controller.GetGameManager().ScreenImpactGround(0.04f, 0.4f);
                AudioManager.Instance.PlaySFX("impact", 0.75f);
                AudioManager.Instance.PlaySFX("Magic Element Thunder 01", 1.2f);
                controller.SpawnSpecialEffect(thunderExplosionEffect, new Vector2(controller.transform.position.x, -1.094363f), false);

                // deal damage
                if (!dealDamageAlready
                    &&
                    (Mathf.Abs(player.transform.position.x - transform.position.x) < 
                    (controller.GetCollider().bounds.size.x * 0.5f) + (player.GetComponent<Collider2D>().bounds.size.x * 0.5f))) //@todo optimization
                {
                    player.StartJump(false, true);
                    if (player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform))
                    {
                        //  cancel previous buff
                        if (!ReferenceEquals(ElectricBuffCoroutine, null))
                        {
                            StopCoroutine(ElectricBuffCoroutine);
                        }

                        // Apply buff into player
                        float buffDuration = 2.5f;
                        ElectricBuffCoroutine = StartCoroutine(ElectricShockBuff(buffDuration, 0.45f));
                        player.DisableDash(buffDuration);
                        dealDamageAlready = true;
                    }
                }

                // decide next action
                jumpCount++;
                if (jumpCount < jumpNumber)
                {
                    controller.TurnTowardPlayer();
                    StartJump();
                }
                else
                {
                    // final SE
                    AudioManager.Instance.PlaySFX("slam", 0.75f);

                    // animator
                    animator.SetBool("Fall", false);
                    animator.SetBool("Jump", false);

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

            // deal damage
            if (!dealDamageAlready
                && 
                ((  Mathf.Abs(player.transform.position.x - transform.position.x) < attackRange * 1.5f
                && Mathf.Abs(player.transform.position.y - transform.position.y) < 3.5f
                && controller.IsFacingPlayer()
                && jumpCount >= jumpNumber)
                ||
                ( Mathf.Abs(player.transform.position.x - transform.position.x) < 2.5f
               && Mathf.Abs(player.transform.position.y - transform.position.y) < 3.5f
               && jumpCount < jumpNumber)))
            {
                player.StartJump(false, true);
                if (player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform))
                {
                    //  cancel previous buff
                    if (!ReferenceEquals(ElectricBuffCoroutine, null))
                    {
                        StopCoroutine(ElectricBuffCoroutine);
                    }

                    // Apply buff into player
                    float buffDuration = 2.5f;
                    ElectricBuffCoroutine = StartCoroutine(ElectricShockBuff(buffDuration, 0.45f));
                    player.DisableDash(buffDuration);
                    dealDamageAlready = true;
                }
            }
        }
    }
}
