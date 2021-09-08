using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Controller : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int baseDamage = 5;
    [SerializeField] private int maxDamage = 5;
    [SerializeField] private int dashDamage = 0;
    [SerializeField] private int hpRegen = 0;
    [SerializeField] private float lifesteal = 0;
    [SerializeField] private int lifedrain = 0;
    [SerializeField] private bool survivor = false;
    [SerializeField] private int comboMaster = 0;
    [SerializeField] private int staminaCostAttack = 15;
    [SerializeField] private int staminaCostDash = 20;
    [SerializeField] private int maxHP = 5;
    [SerializeField] private int maxStamina = 50;
    [SerializeField] private float moveSpeed = 11.0f;
    [SerializeField] private float dashRange = 3.0f;
    [SerializeField] private float dashCooldown = 1.0f;
    [SerializeField] private int staminaRegen = 1;
    [SerializeField] private float staminaRegenInterval = 0.5f;
    [SerializeField] private float pushEnemySpeedMultiplier = 0.15f;
    [SerializeField] private float criticalDamageMultiplier = 1.5f;
    [SerializeField] private float potionSpeed = 1.5f;

    [Header("Parameters")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpPressMemoryTime = 0.2f;
    [SerializeField] private float attackPressMemoryTime = 0.5f;
    [SerializeField] private LayerMask frameLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float comboTimeLast = 1.0f;
    [SerializeField] private float dashTime = 0.4f;
    [SerializeField] private float tailTime = 0.2f;
    [SerializeField, Range(0.0f, 1.0f)] private float initialTailAlpha = 0.5f;
    [SerializeField] private float afterEffectInterval = 0.1f;
    [SerializeField] private float hpRegenInterval = 1f;
    [SerializeField] private float comboResetTime = 1.0f;
    [SerializeField] private Vector2 combatModeRange = new Vector2(3f, 1.5f);
    [SerializeField] private float potionCooldown = 0.5f;

    [SerializeField] private float attackRange;
    [SerializeField] private float[] attackMoveRange;
    [SerializeField] private float[] attackDealTiming;
    [SerializeField] private float[] attackEndTiming;

    Rigidbody2D rigidbody;
    Collider2D collider;
    float dashDirection;
    float dashCooldownTimer;
    float afterEffectCd;
    PlayerInput input;
    float staminaRegenTimer;
    float hpRegenTimer;
    bool alreadyDealDamage;
    float attackDealDamageTimer;
    float attackEndAttackTimer;
    GameManager gameMng;
    bool isKnockbacking;
    bool isUsingPotion;
    bool isAlive;
    float superDropAfterImgTimer;
    float superDropAfterImgInterval = 0.05f;
    float hitTaintTime = 0.2f;
    Color playerColor;
    private GameObject impactParticleEffect;
    private GameObject hitParticleEffect;
    private GameObject hitFXEffect;
    private GameObject jumpDustEffect;

    [Header("References")]
    [SerializeField] SpriteRenderer graphic;
    [SerializeField] Animator animator;
    [SerializeField] AudioSource walkAudio;
    [SerializeField] GameObject reviveParticle;
    [SerializeField] Transform world;

    [Header("States")]
    [SerializeField] private int attackCombo;
    [SerializeField] private bool jumpPressed;
    [SerializeField] private bool attackPressed;
    [SerializeField] private bool jumpCancelled;
    [SerializeField, Range(0.0f, 0.2f)] private float jumpPressMemoryDelay;
    [SerializeField, Range(0.0f, 0.5f)] private float attackPressMemoryDelay;
    [SerializeField, Range(0.0f, 1.0f)] private float comboResetTimer;
    [SerializeField] private int currentCombo;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool isDashing;
    [SerializeField] private bool isJumping;
    [SerializeField] private float comboTimer;
    [SerializeField] private bool attackDisabled;
    [SerializeField] private float dashTimeCount;
    [SerializeField] private int currentHP;
    [SerializeField] private int currentStamina;
    [SerializeField] private bool superDrop;
    [SerializeField] private float hitTaintTimer;
    [SerializeField] int dashFequency;   // frquency of spamming dash
    [SerializeField] float dashStaminaCooldown;
    [SerializeField] private bool invulnerable;
    [SerializeField] private bool staminaRegenSlowed;

    List<EnemyControl> dashDamagedEnemy = new List<EnemyControl>();

    public struct PlayerInput
    {
        // INPUTS
        public float move; 
        public bool jump; 
        public bool cancelJump;
        public bool attack; 
        public bool dash;
        public bool crouch;
        public bool use;
        public bool cancelUse;
    }

    private void Awake()
    {
        impactParticleEffect = Resources.Load("Prefabs/ImpactGround") as GameObject;
        hitParticleEffect = Resources.Load("Prefabs/HitParticle") as GameObject;
        hitFXEffect = Resources.Load("Prefabs/HitFX") as GameObject;
        jumpDustEffect = Resources.Load("Prefabs/JumpDust") as GameObject;
    }

    public void ResetPlayer()
    {
        // stats
        baseDamage = 8;
        maxDamage = 0;
        dashDamage = 0;
        hpRegen = 0;
        lifesteal = 0;
        lifedrain = 0;
        survivor = false;
        comboMaster = 0;
        staminaCostAttack = 20;
        staminaCostDash = 40;
        maxHP = 50;
        maxStamina = 100;
        moveSpeed = 11.0f;
        dashRange = 4.5f;
        dashCooldown = 0.7f;
        staminaRegen = 2;
        staminaRegenInterval = 0.05f;
        pushEnemySpeedMultiplier = 0.15f;
        criticalDamageMultiplier = 1.5f;

        // basic parameter
        isAlive = true;
        jumpPressed = false;
        attackDisabled = false;
        isAttacking = false;
        isDashing = false;
        isJumping = true;
        superDrop = false;

        currentHP = maxHP;
        currentStamina = maxStamina;
        playerColor = Color.white;
    }

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        gameMng = Object.FindObjectOfType<GameManager>();
        isAlive = true;
        jumpPressed = false;
        attackDisabled = false;
        isAttacking = false;
        isDashing = false;
        isJumping = true;
        superDrop = false;

        currentHP = maxHP;
        currentStamina = maxStamina;
        playerColor = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        input.move =        Mathf.RoundToInt(Input.GetAxisRaw("Horizontal")) + Mathf.RoundToInt(Input.GetAxisRaw("JoyPadHorizontal"));
        input.jump =        Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetButtonDown("Jump");
        input.cancelJump =  Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W) || Input.GetButtonUp("Jump");
        input.attack =      Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J) || Input.GetButtonDown("Attack");
        input.dash =        Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("Dash");
        input.crouch =      Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || (Mathf.RoundToInt(Input.GetAxisRaw("JoyPadVertical")) == -1);
        input.use =         Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.L);// || Input.GetButtonDown("Dash");
        input.cancelUse =   Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.L);// || Input.GetButtonDown("Dash");

        if (!isAlive)
        {
            input.move = 0;
            input.jump = false;
            input.cancelJump = false;
            input.attack = false;
            input.dash = false;
        }

        // GRAPHIC
        {
            if (!isAttacking)
            {
                if (input.move > 0)
                {
                    graphic.flipX = false;
                }
                else if (input.move < 0)
                {
                    graphic.flipX = true;
                }
            }

            hitTaintTimer -= Time.deltaTime;
            if (hitTaintTimer >= 0.0f)
            {
                playerColor = new Color(1.0f, 0.5f, 0.5f);
            }
            else if (((float)currentStamina / (float)maxStamina) < 0.2f)
            {
                playerColor = new Color(0.3f, 0.3f, 0.7f);
            }
            else if (invulnerable)
            {
                playerColor = Color.yellow;
            }
            else
            {
                playerColor = Color.white;
            }
            graphic.DOColor(playerColor, 0.5f);

            animator.SetBool("IsJumping", rigidbody.velocity.y > 0.0f);
            animator.SetBool("IsFalling", rigidbody.velocity.y < 0.0f);
            animator.SetBool("MoveX", input.move != 0.0f);
        }

        // JUMP
        {
            // landed
            if (collider.IsTouchingLayers(frameLayer) && rigidbody.velocity.y <= 0f && IsJumping())
            {
                if (superDrop)
                {
                    SuperLanding();
                }

                isJumping = false;
                attackDisabled = false;
                superDrop = false;

                // SE
                AudioManager.Instance.PlaySFX("landing", 0.75f);
                rigidbody.velocity = new Vector2(0.0f, 0.0f);
            }

            // start jump
            if (input.jump || jumpPressed)
            {
                if (!IsJumping())
                {
                    StartJump();
                }
                else if (!jumpPressed)  // reset jump delay timer
                {
                    jumpPressMemoryDelay = jumpPressMemoryTime;
                    jumpPressed = true;
                }
            }

            if (jumpPressed)
            {
                jumpPressMemoryDelay = Mathf.Clamp(jumpPressMemoryDelay - Time.deltaTime, 0.0f, jumpPressMemoryTime);
                if (jumpPressMemoryDelay == 0.0f)
                {
                    jumpPressed = false;
                }
            }

            if (rigidbody.velocity.y > 0.0f && !jumpCancelled)
            {
                if (input.cancelJump)
                {
                    CancelJump();
                }
            }

            if (rigidbody.velocity.y < -55f)
            {
                superDropAfterImgTimer = superDropAfterImgInterval;
                superDrop = true;
            }

            if (superDrop)
            {
                superDropAfterImgTimer += Time.deltaTime;
                if (superDropAfterImgTimer >= superDropAfterImgInterval)
                {
                    superDropAfterImgTimer = 0.0f;
                    CreateAfterImage(initialTailAlpha / 4.0f);
                }
            }
        }

        // ATTACK
        {
            if (isAttacking && IsAlive())
            {
                // deal damage
                if (!alreadyDealDamage)
                {
                    attackDealDamageTimer += Time.deltaTime;

                    if ( attackDealDamageTimer >= attackDealTiming[(Mathf.Max((attackCombo-1), 0) % 3)] )
                    {
                        alreadyDealDamage = true;
                        CheckHitEnemy();
                    }
                }

                // end attack
                attackEndAttackTimer = Mathf.Max(attackEndAttackTimer - Time.deltaTime, 0.0f);

                // collision check
                RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(dashDirection, 0.0f), collider.bounds.size.x, frameLayer);
                if (hit)
                {
                    transform.DOMoveX(transform.position.x, dashTime, false);
                }

                // valocity restriction
                rigidbody.velocity = new Vector2(0.0f, Mathf.Clamp(rigidbody.velocity.y, -0.01f, 0.01f));

                // determine if attack animation is ended
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack0")
                    && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1")
                    && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2")
                    && IsAlive())
                {
                    isAttacking = false;
                    if (attackPressed && !attackDisabled)
                    {
                        StartAttack();
                    }
                }

                if (attackPressed)
                {
                    attackPressMemoryDelay = Mathf.Clamp(attackPressMemoryDelay - Time.deltaTime, 0.0f, attackPressMemoryTime);
                    if (attackPressMemoryDelay == 0.0f)
                    {
                        attackPressed = false;
                    }
                }

                if (attackEndAttackTimer == 0.0f)
                {
                    // allow attack
                    if (input.attack)
                    {
                        StartAttack();
                    }
                }
            }

            // countdown combo time
            if (attackCombo > 0)
            {
                comboTimer = Mathf.Clamp(comboTimer - Time.deltaTime, 0.0f, comboTimeLast);
                if (comboTimer == 0.0f)
                {
                    // reset combo
                    attackCombo = 0;
                }
            }

            // input detected
            if (input.attack)
            {
                if (!isAttacking && !attackDisabled)
                {
                    StartAttack();
                }
                else
                {
                    attackPressed = true;
                    attackPressMemoryDelay = attackPressMemoryTime;
                }
            }
        }

        // DASH
        {
            if (input.dash
                && (!isAttacking || attackEndAttackTimer == 0.0f) && dashCooldownTimer == 0.0f)// && currentStamina > maxStamina /5)
            {
                StartDash();
            }

            if (isDashing)
            {
                dashTimeCount = Mathf.Clamp(dashTimeCount - Time.deltaTime, 0.0f, dashTime);
                if (dashTimeCount == 0.0f)
                {
                    EndDash();
                }
                else
                {
                    if (dashDamage > 0) CheckHitEnemy(true);

                    // collision check
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(dashDirection, 0.0f), collider.bounds.size.x, frameLayer);
                    if (hit)
                    {
                        transform.DOMoveX(transform.position.x, dashTime, false);
                    }
                }
            }
            else if (dashCooldownTimer > 0.0f)  // not dashing
            {
                // cooldown
                dashCooldownTimer = Mathf.Clamp(dashCooldownTimer - Time.deltaTime, 0.0f, dashCooldown);
                
            }

            if (dashStaminaCooldown > 0.0f)
            {
                dashStaminaCooldown = Mathf.Clamp(dashStaminaCooldown - Time.deltaTime, 0.0f, dashStaminaCooldown);
                if (dashStaminaCooldown == 0.0f)
                {
                    dashFequency = 0;
                }
            }
        }

        // CROUCH
        {
            if (input.crouch && input.move == 0)
            {
                if (!IsJumping() && !IsDashing() && !IsAttacking() && !IsUsingPotion())
                {
                    animator.SetBool("Crouch", true);
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch"))
                    {
                        animator.Play("Crouch");
                    }
                }
            }
            else
            {
                animator.SetBool("Crouch", false);
            }
        }

        // USE POTION
        {
            if (input.use)
            {
                if (!IsJumping() && !IsDashing() && !IsAttacking() && !input.crouch && input.move == 0)
                {
                    // Start Using Potion
                    isUsingPotion = true;
                    animator.SetBool("Use", true);
                    animator.Play("Use");
                }
            }

            else
            {
                animator.SetBool("Use", false);
            }
        }

        // COMBO
        if (currentCombo > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0)
            {
                gameMng.ResetCombo(currentCombo);
                if (comboMaster > 0 && currentCombo > 0)
                {
                    Regenerate(0, comboMaster * currentCombo);
                    AudioManager.Instance.PlaySFX("comboMaster");
                    gameMng.SpawnFloatingText( new Vector2(transform.position.x , transform.position.y + collider.bounds.size.y / 2f) , 2f, 25f,
                                                (comboMaster * currentCombo).ToString(), Color.blue, new Vector2(0,1), 80f);
                }
                currentCombo = -1;
            }
        }
    }

    private void FixedUpdate()
    {
        // MOVE
        {
            float calculatedMoveSpeed = moveSpeed;
            if (currentStamina <= maxStamina / 5f)
            {
                calculatedMoveSpeed = moveSpeed / 2f;
            }
            if (IsJumping()) calculatedMoveSpeed = Mathf.Clamp(calculatedMoveSpeed, 0f, 10f);
            RaycastHit2D hitwall = Physics2D.Raycast(transform.position, new Vector2(input.move, 0.0f), collider.bounds.size.x, frameLayer);
            RaycastHit2D hitenemy = Physics2D.Raycast(transform.position, new Vector2(input.move, 0.0f), collider.bounds.size.x * 0.5f, enemyLayer);
            if (!hitwall && !isAttacking && !isDashing)
            {
                float multiplier = 1.0f;
                if (hitenemy && hitenemy.transform.GetComponent<EnemyControl>().IsAlive() 
                    && ((transform.position.x > hitenemy.transform.position.x && input.move == -1) || (transform.position.x < hitenemy.transform.position.x && input.move == 1))
                    && !hitenemy.transform.GetComponent<EnemyControl>().IsImmumetoKnockback()
                    && !hitenemy.transform.GetComponent<EnemyControl>().IsAlwaysIgnoreKnockback())
                {
                    multiplier = pushEnemySpeedMultiplier;
                    hitenemy.transform.DOMoveX(hitenemy.transform.position.x + (moveSpeed * input.move * Time.deltaTime * multiplier), 0.1f, false);
                }

                if (input.move != 0 && !isKnockbacking)
                {
                    transform.DOMoveX(transform.position.x + (calculatedMoveSpeed * input.move * Time.deltaTime * multiplier), 0.1f, false);
                }
            }

            walkAudio.mute = (input.move == 0 || IsJumping() || IsDashing() || IsAttacking());
            if (!walkAudio.mute && !walkAudio.isPlaying)
            {
                walkAudio.Play();
            }
            else if (walkAudio.mute && walkAudio.isPlaying)
            {
                walkAudio.Stop();
            }
        }

        // REGEN
        {
            //CHECK IF THERE ARE MONSTER AROUND
            bool monsterNearby = false;
            List<EnemyControl> enemies = gameMng.GetMonsterList();
            foreach (EnemyControl enemy in enemies)
            {
                if (Mathf.Abs(transform.position.x - enemy.transform.position.x) < combatModeRange.x &&
                    Mathf.Abs(transform.position.y - enemy.transform.position.y) < combatModeRange.y * 1.75f &&
                    enemy.IsAlive())
                {
                    monsterNearby = true;
                    continue;
                }
            }

            float multiplier = 1.0f;
            if (dashFequency > 2)  multiplier /= dashFequency+1;
            if (IsAttacking())     multiplier = 0.0f;
            if (IsDashing())       multiplier *= 0.5f;
            if (input.move != 0)   multiplier *= 0.8f;
            if (IsJumping())       multiplier *= 0.5f;
            if (monsterNearby)     multiplier = 0.2f;
            staminaRegenTimer = Mathf.Clamp(staminaRegenTimer - (Time.deltaTime * multiplier), 0.0f, staminaRegenInterval);
            if (staminaRegenTimer == 0.0f)
            {
                staminaRegenTimer = staminaRegenInterval;
                Regenerate(0, staminaRegen);
            }
            staminaRegenSlowed = (multiplier <= 0.2f);

            multiplier = 1.0f;
            //if (IsDashing()) multiplier = 0.0f;
            //if (IsJumping()) multiplier = 0.0f;
            if (monsterNearby) multiplier = 0.0f;
            if (IsAttacking()) multiplier = 0.0f;
            if (!IsAlive())    multiplier = 0.0f;
            if (gameMng.IsLevelEnded()) multiplier = 0.0f;
            if (gameMng.IsLevelEnded()) multiplier = 0.0f;
            hpRegenTimer = Mathf.Clamp(hpRegenTimer - (Time.deltaTime * multiplier), 0.0f, hpRegenInterval);
            if (hpRegenTimer == 0.0f)
            {
                hpRegenTimer = hpRegenInterval;
                Regenerate(hpRegen);
            }
        }
    }

    public void StartJump(bool reviveJump = false)
    {
        // set flag
        jumpCancelled = false;
        jumpPressed = false;
        isJumping = true;
        attackCombo = 0;

        // leave collision
        rigidbody.velocity = new Vector2(0f, 0.1f);

        // FX
        if (!reviveJump) Instantiate(jumpDustEffect, new Vector2(transform.position.x, -2.619f), Quaternion.identity).transform.SetParent(world);

        // determine which type of jump this is
        if (reviveJump) // this is a revive jump
        {
            rigidbody.AddForce(new Vector2(0.0f, jumpForce * 1.2f));
            
        }
        else if (!input.jump)   // this is a delayed jump
        {
            jumpCancelled = true;
            rigidbody.AddForce(new Vector2(0.0f, jumpForce / 2f));
        }
        else if ((float)currentStamina / (float)maxStamina < 0.2f)   // no stamina
        {
            rigidbody.AddForce(new Vector2(0.0f, jumpForce / 2f));
        }
        else // standard jump
        {
            rigidbody.AddForce(new Vector2(0.0f, jumpForce));
        }

        // SE
        AudioManager.Instance.PlaySFX("jump");

        gameMng.PlayerJumped();
    }

    void CancelJump()
    {
        jumpCancelled = true;
        rigidbody.velocity = new Vector2(rigidbody.velocity.x, rigidbody.velocity.y / 2.0f);
    }

    void StartAttack()
    {
        // check if stamina is enough
        if (currentStamina < staminaCostAttack)
        {
            // reset combo if stamina is all gone
            attackCombo = 0;
            return;
        }

        // dash attack
        if (IsDashing())
        {
            transform.DOKill(true);
            dashDirection = graphic.flipX ? -1 : 1;
            EndDash();
            attackCombo = 2;
        }

        Regenerate(0, -staminaCostAttack);

        alreadyDealDamage = false;
        attackEndAttackTimer = FindAnimation(animator, "Attack" + (attackCombo % 3).ToString()).length * attackEndTiming[(attackCombo % 3)];
        attackDealDamageTimer = 0.0f;
        attackPressed = false;
        isAttacking = true;
        animator.Play("Attack" + (attackCombo % 3).ToString());
        comboTimer = comboTimeLast;

        dashDirection = graphic.flipX ? -1 : 1;
        float multiplier = rigidbody.velocity.y == 0.0f ? 1f : 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(dashDirection, 0.0f), collider.bounds.size.x, frameLayer);
        RaycastHit2D hitenemy = Physics2D.Raycast(transform.position, new Vector2(input.move, 0.0f), collider.bounds.size.x, enemyLayer);

        if (hitenemy)
        {
            multiplier *= 0.25f;
        }

        if (!isKnockbacking)
        {
            if (hit)
            {
                transform.DOMoveX(transform.position.x, dashTime, false);
            }
            else
            {
                transform.DOMoveX(transform.position.x + dashDirection * attackMoveRange[attackCombo % 3] * multiplier, animator.GetCurrentAnimatorStateInfo(0).length / 2.0f, false);
            }
        }

        // audio
        AudioManager.Instance.PlaySFX("sword" + (attackCombo % 3).ToString());

        // code that must be on the last
        attackCombo++;

        // air attack restrictions
        if (IsJumping())
        {
            if (attackCombo > 2)
            {
                attackDisabled = true;
            }
        }
    }

    void CheckHitEnemy(bool isDash = false)
    {
        List<EnemyControl> enemyList = gameMng.GetMonsterList();

        int direction = graphic.flipX ? -1 : 1;
        Vector2 playerAttackOriginPoint = new Vector2(transform.position.x + (attackRange * direction), transform.position.y);
        if (isDash)
        {
            playerAttackOriginPoint = transform.position;
        }

        bool isHitEnemy = false;
        bool isCriticalHit = false;
        bool isDefended = false;
        if (enemyList.Count > 0)
        {
            foreach (EnemyControl enemy in enemyList)
            {
                Transform target = enemy.transform;
                float calculatedAttackRange = attackRange;
                if (isDash) calculatedAttackRange /= 2.25f;

                float enemyHitLeft = target.position.x + (enemy.GetCollider().bounds.size.x / 2f * -direction);
                float enemyHitRight = target.position.x + (enemy.GetCollider().bounds.size.x / 2f * direction);
                Debug.DrawLine(new Vector2(playerAttackOriginPoint.x, playerAttackOriginPoint.y), new Vector2(enemyHitLeft, target.position.y), Color.red, 0.5f);
                Debug.DrawLine(new Vector2(playerAttackOriginPoint.x, playerAttackOriginPoint.y), new Vector2(enemyHitRight, target.position.y), Color.red, 0.5f);
                if ((Mathf.Abs(enemyHitLeft - playerAttackOriginPoint.x) < calculatedAttackRange || Mathf.Abs(enemyHitRight - playerAttackOriginPoint.x) < calculatedAttackRange)
                    && ((Mathf.Abs(target.position.y - playerAttackOriginPoint.y) < calculatedAttackRange * 0.75f )
                    || (Mathf.Abs((target.position.y + enemy.GetHeight()/2f) - playerAttackOriginPoint.y) < calculatedAttackRange * 0.75f ))
                    && enemy.IsAlive()
                    && !enemy.IsInvulnerable()
                    && (!isDash || !dashDamagedEnemy.Contains(enemy)))
                {
                    // calculate damage
                    int calculatedDamage = baseDamage + (Random.Range(0, maxDamage));

                    // check is third attack
                    calculatedDamage = attackCombo % 3 == 0 ? (int)((float)calculatedDamage * 1.5f) : calculatedDamage;

                    // check critical
                    isCriticalHit = enemy.CheckIfCriticalHit(playerAttackOriginPoint);
                    if (isCriticalHit)
                    {
                        calculatedDamage = Mathf.FloorToInt((float)calculatedDamage * criticalDamageMultiplier);
                    }

                    // check dash
                    if (isDash)
                    {
                        calculatedDamage = dashDamage;
                        isCriticalHit = false;
                        dashDamagedEnemy.Add(enemy);
                    }

                    // check defend
                    isDefended = enemy.CheckIfDefended(playerAttackOriginPoint);
                    if (isDefended)
                    {
                        AudioManager.Instance.PlaySFX("defend" + Random.Range(0, 4).ToString(), 0.8f);
                        enemy.DefendDamage();
                        continue;
                    }

                    // apply damage
                    bool kill = enemy.DealDamage(calculatedDamage);

                    // apply lifesteal
                    if (lifesteal > 0.0f)
                    {
                        Regenerate((int)(((float)calculatedDamage) * lifesteal));
                    }

                    // apply lifedrain
                    if (lifedrain > 0 && kill)
                    {
                        Regenerate(lifedrain);
                    }

                    // calculate knockback distance
                    float baseDistance = attackMoveRange[(Mathf.Max((attackCombo - 1), 0) % 3)];
                    float multiplier = 1.0f * ((Mathf.Abs(target.position.x - transform.position.x)/4f) / (baseDistance));
                    enemy.Knockback(baseDistance * multiplier, direction, 0.15f);

                    isHitEnemy = true;

                    // create particle effect
                    GameObject particle = Instantiate(hitParticleEffect, Vector2.Lerp(transform.position, enemy.transform.position, 0.5f) , Quaternion.identity);
                    particle.GetComponent<ParticleScript>().SetParticleColor(gameMng.GetThemeColor());
                    particle.transform.SetParent(world, true);
                    particle = Instantiate(hitFXEffect, Vector2.Lerp(transform.position, enemy.transform.position, Random.RandomRange(0.6f, 0.9f)), Quaternion.identity);
                    particle.transform.eulerAngles = new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f),  Random.Range(-180f, 180f));
                    particle.transform.SetParent(world, true);

                    // create floating text
                    Color fontColor = Color.white;
                    float floatSize = Mathf.Clamp(50 * (calculatedDamage / baseDamage), 0, 80);
                    string extraText = string.Empty;

                    if (isCriticalHit)
                    {
                        floatSize *= 1.2f;
                        extraText = "!";
                        fontColor = new Color(1.0f, 0.75f, 0.0f);   // orange
                    }

                    Vector2 randomize = new Vector2(Random.Range(-enemy.GetComponent<Collider2D>().bounds.size.x / 2f, enemy.GetComponent<Collider2D>().bounds.size.x / 2f), Random.Range(-0.5f, 0.5f));
                    Vector2 floatDirection = new Vector2(enemy.transform.position.x - transform.position.x, transform.position.y - enemy.transform.position.y);
                    gameMng.SpawnFloatingText(new Vector2(enemy.transform.position.x, enemy.transform.position.y + enemy.GetComponent<Collider2D>().bounds.size.y * 0.75f) + randomize
                                                 , 2f + Random.Range(0.0f, 1.0f), 25f + Random.Range(0.0f, 25.0f), 
                                                 calculatedDamage.ToString() + extraText, fontColor, floatDirection.normalized, floatSize);
                }
            }
        }

        if (isHitEnemy)
        {
            if (!isDash)
            {
                transform.DOMoveX(transform.position.x, dashTime, false);
            }

            if (isCriticalHit)
            {
                AudioManager.Instance.PlaySFX("criticalhit");
            }
            else
            {
                AudioManager.Instance.PlaySFX("hit");
            }

            currentCombo++;
            comboResetTimer = comboResetTime;
            gameMng.SetCombo(currentCombo);
        }
    }

    void StartDash()
    {
        dashDamagedEnemy.Clear();
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        dashStaminaCooldown = Mathf.Min(dashStaminaCooldown + dashCooldown * 2f , 3f);
        dashFequency++;
        animator.Play("Dash");
        dashDirection = graphic.flipX ? -1 : 1;

        dashTimeCount = dashTime;

        graphic.sortingLayerName = "Default";

        if (rigidbody.velocity.y < 0.0f)
        {
            rigidbody.velocity = new Vector2(0, 0);
        }

        float calculateddashrange = dashRange;
        float calculateddashtime = dashTime;
        if ((float)currentStamina / (float)maxStamina < 0.2f)
        {
            calculateddashrange *= 0.5f;
            calculateddashtime *= 1.1f;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(dashDirection, 0.0f), calculateddashrange, frameLayer);
        if (hit)
        {
            float newRange = Mathf.Abs(transform.position.x - (hit.point.x + (-dashDirection * collider.bounds.size.x)));
            transform.DOMoveX(hit.point.x + (-dashDirection * collider.bounds.size.x), calculateddashtime * (newRange / calculateddashrange), false);
        }
        else
        {
            transform.DOMoveX(transform.position.x + (dashDirection * calculateddashrange), calculateddashtime, false);
        }

        AfterEffectDuration(afterEffectInterval, calculateddashtime);

        // SE
        AudioManager.Instance.PlaySFX("dash", 0.85f);

        // CALCULATE STAMINA COST AT THE LAST
        Regenerate(0, -staminaCostDash);
    }

    private void EndDash()
    {
        isDashing = false;

        graphic.sortingLayerName = "Player";
    }

    public void CreateAfterImage(float alpha)
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
        afterImg.GetComponent<Tail>().Initialization(tailTime, tailRenderer, alpha);
        //--- done ---
        Destroy(afterImg, tailTime);
    }

    public AnimationClip FindAnimation(Animator _animator, string name)
    {
        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
            {
                return clip;
            }
        }

        return null;
    }

    public int GetCurrentHP()
    {
        return currentHP;
    }
    public int GetMaxHP()
    {
        return maxHP;
    }
    public int GetCurrentStamina()
    {
        return currentStamina;
    }
    public int GetMaxStamina()
    {
        return maxStamina;
    }

    public void Regenerate(int hp, int stamina = 0)
    {
        currentHP = Mathf.Clamp(currentHP + hp, 0, maxHP);
        currentStamina = Mathf.Clamp(currentStamina + stamina, 0, maxStamina);

        if (hp > 0)
        {
            // floating text
            const float floatSize = 90;
            Vector2 randomize = new Vector2(Random.Range(-collider.bounds.size.x / 2f, collider.bounds.size.x / 2f), Random.Range(-0.5f, 0.5f));
            gameMng.SpawnFloatingText(new Vector2(transform.position.x, transform.position.y) + randomize
                                         , 4f + Random.Range(1.0f, 3.0f), 25f + Random.Range(0.0f, 25.0f),
                                         hp.ToString(), new Color(0.0f, 0.80f, 0.0f), new Vector2(0f, 1f), floatSize);
        }
    }

    public void RegeneratePercentage(float hp, float stamina = 0.0f)
    {
        int amount = Mathf.RoundToInt((float)maxHP * hp);
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);

        amount = Mathf.RoundToInt((float)maxStamina * stamina);
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
    }

    public void DamagePercentage(float hp, float stamina = 0.0f)
    {
        int amount = Mathf.RoundToInt((float)maxHP * hp);
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);

        amount = Mathf.RoundToInt((float)maxStamina * stamina);
        currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
    }

    public bool DealDamage(int value, Transform source)
    {
        if (!isAlive) return false;
        if (isDashing) return false;
        if (invulnerable) return false;
        if (value <= 0) return false;

        gameMng.ResetCombo(currentCombo);
        currentCombo = -1;

        currentHP = Mathf.Clamp(currentHP - value, 0, maxHP);

        const float floatSize = 100;
        int direction = source.position.x > transform.position.x ? -1 : 1;
        Vector2 randomize = new Vector2(Random.Range(-collider.bounds.size.x / 2f, collider.bounds.size.x / 2f), Random.Range(-0.5f, 0.5f));
        gameMng.SpawnFloatingText(new Vector2(transform.position.x, transform.position.y) + randomize
                                     , 4f + Random.Range(1.0f, 3.0f), 25f + Random.Range(0.0f, 25.0f),
                                     value.ToString(), new Color(0.75f, 0.0f, 0.0f), new Vector2(direction * 1f, 1f), floatSize);


        hitTaintTimer = hitTaintTime;

        if (input.move == 0)
        {
            animator.Play("Hurt");
        }

        Knockback(((float)value / (float)Mathf.Max(maxHP, 1)) * 10f, direction, 0.1f);

        // check is dead
        if (currentHP == 0)
        {
            // dead
            graphic.flipX = direction == 1;
            animator.Play("Dead");
            isAlive = false;
            AudioManager.Instance.PlaySFX("criticalplayerHit", 0.8f);
            StartCoroutine(Dead(FindAnimation(animator, "Dead").length));
            return true;
        }

        AudioManager.Instance.PlaySFX("playerHit", 0.8f);
        return true;
    }

    IEnumerator Dead(float waitTime)
    {
        yield return new WaitForSeconds(waitTime/2f);
        AudioManager.Instance.PlaySFX("dead", 0.85f);
        yield return new WaitForSeconds(waitTime/2f);

        if (survivor)
        {
            // revive
            Instantiate(reviveParticle, new Vector2(transform.position.x, -1.14f), Quaternion.identity);
            AudioManager.Instance.PlaySFX("revive");
            animator.Play("Jump");
            isAlive = true;
            survivor = false;
            StartJump(true);
            currentHP = maxHP;
            AfterEffectDuration(afterEffectInterval, 0.5f);
            gameMng.SpawnFloatingText(new Vector2(transform.position.x, transform.position.y + collider.bounds.size.y / 2f), 5f, 30f,
                                     "RESURRECTION", new Color(1f, 8.43f, 0.0f), new Vector2(0f, 1f), 100f);

            graphic.color = Color.yellow;
            StartCoroutine(InvulnerableForSeconds(1.0f));
        }
        else
        {
            // end game
            yield return new WaitForSeconds(0.5f);
            gameMng.GameOver();
        }
    }

    IEnumerator InvulnerableForSeconds(float time)
    {
        invulnerable = true;

        yield return new WaitForSeconds(time);

        invulnerable = false;
    }

    public void Knockback(float value, int direction, float time)
    {
        // face toward the source of knockback
        if (!IsAttacking() && !IsDashing() && !IsJumping())
        {
            graphic.flipX = direction == 1;
        }

        // loop
        StartCoroutine(KnockbackLoop(value, direction, time));
    }

    IEnumerator KnockbackLoop(float value, float dir, float time)
    {
        float timeElapsed = 0.0f;
        isKnockbacking = true;
        while (timeElapsed < time)
        {
            timeElapsed += Time.deltaTime;
            RaycastHit2D hitwall = Physics2D.Raycast(transform.position, new Vector2((dir * Time.deltaTime * value), 0.0f), collider.bounds.size.x, frameLayer);
            if (!hitwall)
            {
                transform.DOMoveX(transform.position.x + (dir * Time.deltaTime * value), Time.deltaTime, false);
            }
            yield return null;
        }
        isKnockbacking = false;
    }

    public void AfterEffectDuration(float interval, float duration)
    {
        StartCoroutine(AfterEffectDurationLoop(interval, duration));
    }

    IEnumerator AfterEffectDurationLoop(float interval, float duration)
    {
        float timeElapsed = duration;
        isKnockbacking = true;
        float intervalCnt = 0f;
        while (timeElapsed > 0.0f)
        {
            timeElapsed -= Time.deltaTime;
            intervalCnt += Time.deltaTime;
            if (intervalCnt > interval)
            {
                intervalCnt = 0.0f;
                CreateAfterImage(initialTailAlpha);
            }
            yield return null;
        }
        isKnockbacking = false;
    }

    void SuperLanding()
    {
        gameMng.ScreenImpactGround(0.04f, 0.4f);
        gameMng.ScreenChangeTheme();
        GameObject tmp = Instantiate(impactParticleEffect, Vector2.Lerp(transform.position, new Vector2(transform.position.x, transform.position.y - collider.bounds.size.y /2f),0.5f)
            , Quaternion.identity);
        tmp.GetComponent<ParticleScript>().SetParticleColor(gameMng.GetThemeColor());
        AudioManager.Instance.PlaySFX("impact");
    }
    
    public void ApplyBonus(Skill skill, float value)
    {
        switch (skill)
        {
            case Skill.MoveSpeed:
                moveSpeed += value;
                break;
            case Skill.BaseDamage:
                baseDamage += (int)value;
                break;
            case Skill.MaxDamage:
                maxDamage += (int)value;
                break;
            case Skill.Vitality:
                maxHP += (int)value;
                currentHP += (int)value;
                break;
            case Skill.DashCooldown:
                dashCooldown = dashCooldown - (dashCooldown * (value/100f));
                break;
            case Skill.DashDamage:
                dashDamage += (int)value;
                dashCooldown += 0.5f;
                break;
            case Skill.Stamina:
                maxStamina += (int)value;
                currentStamina+= (int)value;
                break;
            case Skill.StaminaRecoverSpeed:
                staminaRegen += (int)value;
                break;
            case Skill.HPRegen:
                hpRegen += (int)value;
                break;
            case Skill.Lifesteal:
                lifesteal += value/100f;
                break;
            case Skill.LifeDrain:
                lifedrain += (int)value;
                break;
            case Skill.Survivor:
                survivor = true;
                break;
            case Skill.ComboMaster:
                comboMaster += (int)value;
                break;
                Debug.Log("<color=red>skill data not found!</color>");
            default:
                break;
        }
    }

    public bool IsUsingPotion()
    {
        return isUsingPotion;
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    public bool IsJumping()
    {
        return isJumping;
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public bool IsAlive()
    {
        return isAlive;
    }    

    public bool IsStaminaRegenerateSlowed()
    {
        return staminaRegenSlowed;
    }

    public void Pause(bool boolean)
    {
        if (boolean)
        {
            enabled = false;
        }
        else
        {
            enabled = true;
        }
    }

    public int GetHPRegen()
    {
        return hpRegen;
    }
    
    public int GetStaminaMax()
    {
        return maxStamina;
    }

    public int GetStaminaRegen()
    {
        return staminaRegen;
    }
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
    public int GetAttackDamage()
    {
        return baseDamage;
    }
    public int GetMaxDamage()
    {
        return maxDamage;
    }
    public int GetDashDamage()
    {
        return dashDamage;
    }
    public int GetCritical()
    {
        return 0;
    }
    public float GetLifesteal()
    {
        return lifesteal;
    }

    public int GetLifeDrain()
    {
        return lifedrain;
    }
    public int GetComboMaster()
    {
        return comboMaster;
    }
    public float GetDashCD()
    {
        return dashCooldown;
    }
    public bool GetSurvivor()
    {
        return survivor;
    }
    public bool IsFlip()
    {
        return graphic.flipX;
    }
}
