using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OPS.AntiCheat;
using OPS.AntiCheat.Field;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    // プレイヤーのアクションを管理
    public struct PlayerInput
    {
        public float move;
        public bool jump;
        public bool cancelJump;
        public bool attack;
        public bool dash;
        public bool crouch;
        public bool use;
        public bool cancelUse;
    }

    // ステータス
    [Header("Stats")]
    [SerializeField] private ProtectedUInt16 baseDamage = 5;
    [SerializeField] private ProtectedUInt16 maxDamage = 5;
    [SerializeField] private ProtectedUInt16 dashDamage = 0;
    [SerializeField] private ProtectedUInt16 hpRegen = 0;
    [SerializeField] private ProtectedUInt16 berserker = 0;
    [SerializeField] private ProtectedFloat lightningLash = 0;
    [SerializeField] private ProtectedUInt16 lifedrain = 0;
    [SerializeField] private bool survivor = false;
    [SerializeField] private bool deflect = false;
    [SerializeField] private bool battlecry = false;
    [SerializeField] private ProtectedUInt16 comboMaster = 0;
    [SerializeField] private ProtectedFloat echo = 0;
    [SerializeField] private ProtectedUInt16 juggernaut = 0;
    [SerializeField] private ProtectedFloat windrunner = 0;
    [SerializeField] private ProtectedUInt16 breakfallCost = 0;
    [SerializeField] private ProtectedUInt16 potionHeal = 0;
    [SerializeField] private ProtectedUInt16 staminaCostAttackBase = 2;
    [SerializeField] private ProtectedUInt16 staminaCostDash = 10;
    [SerializeField] private ProtectedUInt16 maxHP = 5;
    [SerializeField] private ProtectedUInt16 maxStamina = 50;
    [SerializeField] private ProtectedFloat moveSpeed = 11.0f;
    [SerializeField] private ProtectedFloat dashRange = 3.0f;
    [SerializeField] private ProtectedFloat dashCooldown = 1.0f;
    [SerializeField] private ProtectedFloat dashRechargeTime = 3.0f;
    [SerializeField] private ProtectedUInt16 staminaRegen = 1;
    [SerializeField] private ProtectedFloat staminaRegenInterval = 0.5f;
    [SerializeField] private ProtectedFloat pushEnemySpeedMultiplier = 0.15f;
    [SerializeField] private ProtectedFloat criticalDamageMultiplier = 1.5f;
    [SerializeField] private ProtectedFloat potionSpeed = 1.5f;
    [SerializeField] private ProtectedUInt16 maxDashCharge = 5;

    // プレイヤーキャラ設定
    [Header("Parameters")]
    [SerializeField] private ProtectedFloat jumpForce = 650;
    [SerializeField] private ProtectedFloat jumpPressMemoryTime = 0.2f;
    [SerializeField] private ProtectedFloat attackPressMemoryTime = 0.25f;
    [SerializeField] private ProtectedFloat dashPressMemoryTime = 0.3f;
    [SerializeField] private LayerMask frameLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private ProtectedFloat comboTimeLast = 0.7f;
    [SerializeField] private ProtectedFloat dashTime = 0.4f;
    [SerializeField] private ProtectedFloat tailTime = 0.2f;
    [SerializeField, Range(0.0f, 1.0f)] private float initialTailAlpha = 0.5f;
    [SerializeField] private ProtectedFloat afterEffectInterval = 0.05f;
    [SerializeField] private ProtectedFloat comboResetTime = 1.0f;
    [SerializeField] private ProtectedInt16 maxAttackComboPattern = 3;
    [SerializeField] private Vector2 combatModeRange = new Vector2(3f, 1.5f);
    [SerializeField] private ProtectedFloat potionCooldown = 0.5f;
    [SerializeField, Range(0.0f, 1.5f)] float windrunnerBuffTimer;
    [SerializeField] private ProtectedFloat staminaRegenDelay = 1.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float staminaRegenDelayCounter = 1.0f;

    [SerializeField] private ProtectedFloat attackRange = 1.3f;
    [SerializeField] private float[] attackMoveRange;
    [SerializeField] private float[] attackDealTiming;
    [SerializeField] private float[] attackEndTiming;

    // 他のスクリプトのレファレンス
    [Header("References")]
    [SerializeField] SpriteRenderer graphic;
    [SerializeField] Animator animator;
    [SerializeField] AudioSource walkAudio;
    [SerializeField] GameObject reviveParticle;
    [SerializeField] Transform world;
    [SerializeField] Transform potionProgressBar;
    [SerializeField] SpriteRenderer potionProgressBarFill;
    [SerializeField] TouchController touchMovement;
    [SerializeField] TouchScreenClick attackTouch;
    [SerializeField] TouchScreenClick jumpTouch;
    [SerializeField] TouchScreenClick dashTouch;
    [SerializeField] TouchScreenClick itemTouch;

    // プレイヤーの状態管理・Ｄｅｂｕｇ用
    [Header("States")]
    [SerializeField] private int attackCombo;
    [SerializeField] private bool jumpPressed;
    [SerializeField] private bool attackPressed;
    [SerializeField] private bool jumpCancelled;
    [SerializeField] private ProtectedFloat staminaRecoverDisabledTime;
    [SerializeField, Range(0.0f, 0.2f)] private float jumpPressMemoryDelay;
    [SerializeField, Range(0.0f, 0.5f)] private float attackPressMemoryDelay;
    [SerializeField, Range(0.0f, 0.5f)] private float dashPressMemoryDelay;
    [SerializeField, Range(0.0f, 1.0f)] private float comboResetTimer;
    [SerializeField] private int currentCombo;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool isDashing;
    [SerializeField] private bool isJumping;
    [SerializeField] private bool isKnockbacking;
    [SerializeField] private float comboTimer;
    [SerializeField] private bool attackDisabled;
    [SerializeField] private float dashTimeCount;
    [SerializeField] private int currentHP;
    [SerializeField] private int currentStamina;
    [SerializeField] private int currentDashCharge;
    [SerializeField] private bool superDrop;
    [SerializeField] private float hitTaintTimer;
    [SerializeField] private bool invulnerable;
    [SerializeField] private bool staminaRegenSlowed;
    [SerializeField] private bool islightningLashAttack;

    List<EnemyControl> dashDamagedEnemy = new List<EnemyControl>();

    Rigidbody2D rigidbody;
    Collider2D collider;
    ProtectedFloat dashDirection;
    ProtectedFloat dashCooldownTimer;
    ProtectedFloat dashRechargeTimer;
    ProtectedFloat afterEffectCd;
    ProtectedFloat staminaRegenTimer;
    bool alreadyDealDamage;
    ProtectedFloat attackDealDamageTimer;
    ProtectedFloat attackEndAttackTimer;
    GameManager gameMng;
    bool isUsingPotion;
    bool isPotionUsed;
    bool isAlive;
    bool isJumpCancellable;
    ProtectedFloat usingPotionTime;
    ProtectedFloat superDropAfterImgTimer;
    ProtectedFloat superDropAfterImgInterval = 0.05f;
    ProtectedFloat hitTaintTime = 0.2f;
    Color playerColor;
    bool deflectSucceed;
    private float moveAnimFrame = 0.0f;

    // 入力関連
    private PlayerAction playerInput;
    PlayerInput input;

    // 登録Prefab
    private GameObject impactParticleEffect;
    private GameObject hitParticleEffect;
    private GameObject hitFXEffect;
    private GameObject jumpDustEffect;
    private GameObject shieldEffect;
    private GameObject potionHealEffect;
    private GameObject lightningLashEffect;
    private GameObject chargeEffect;
    private GameObject chargeEffectInstantiated;
    private GameObject bloodEffect;
    private GameObject holyShieldEffect;
    private GameObject holySwordEffect;

    private void Awake()
    {
        // リソースをロード
        shieldEffect         = Resources.Load("Prefabs/ShieldPlayer") as GameObject;
        impactParticleEffect = Resources.Load("Prefabs/ImpactGround") as GameObject;
        hitParticleEffect    = Resources.Load("Prefabs/HitParticle") as GameObject;
        hitFXEffect          = Resources.Load("Prefabs/HitFX") as GameObject;
        jumpDustEffect       = Resources.Load("Prefabs/JumpDust") as GameObject;
        chargeEffect         = Resources.Load("Prefabs/ChargePlayer") as GameObject;
        potionHealEffect     = Resources.Load("Prefabs/PotionHeal") as GameObject;
        lightningLashEffect  = Resources.Load("Prefabs/LightningSlashPlayer") as GameObject;
        bloodEffect          = Resources.Load("Prefabs/BloodPlayer") as GameObject;
        holyShieldEffect     = Resources.Load("Prefabs/HolyShield") as GameObject;
        holySwordEffect      = Resources.Load("Prefabs/HolySword") as GameObject;

        // 入力ボタンを登録
        // INPUT SYSTEM
        playerInput = new PlayerAction();
        playerInput.PlayerControls.Move.performed += ctx => input.move = Mathf.RoundToInt(ctx.ReadValue<float>());
        playerInput.PlayerControls.Move.canceled += ctx => input.move = 0;
        playerInput.PlayerControls.Jump.performed += ctx => input.jump = true;
        playerInput.PlayerControls.Jump.canceled += ctx => input.cancelJump = true;
        playerInput.PlayerControls.Attack.performed += ctx => input.attack = true;
        playerInput.PlayerControls.Dash.performed += ctx => input.dash = true;
        playerInput.PlayerControls.Crouch.performed += ctx => input.crouch = true;
        playerInput.PlayerControls.Crouch.canceled += ctx => input.crouch = false;
        playerInput.PlayerControls.UseItem.performed += ctx => input.use = true;
        playerInput.PlayerControls.UseItem.canceled += ctx => input.cancelUse = true;
    }

    private void OnEnable()
    {
        playerInput.Enable();
        isKnockbacking = false;
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    /// <summary>
    /// プレイヤーの状態をリセット・ゲーム再開用
    /// </summary>
    public void ResetPlayer()
    {
        // キャラの初期ステータス
        // stats
        baseDamage = 8;
        maxDamage = 0;
        dashDamage = 0;
        hpRegen = 0;
        berserker = 0;
        lightningLash = 0;
        lifedrain = 0;
        survivor = false;
        deflect = false;
        battlecry = false;
        comboMaster = 0;
        echo = 0;
        staminaCostAttackBase = 2;
        staminaCostDash = 25;
        maxHP = 50;
        maxStamina = 100;
        moveSpeed = 11.0f;
        dashRange = 4.5f;
        dashCooldown = 1f;
        staminaRegen = 2;
        staminaRegenInterval = 0.05f;
        pushEnemySpeedMultiplier = 0.15f;
        criticalDamageMultiplier = 1.5f;
        potionHeal = 0;
        windrunner = 0.0f;
        breakfallCost = 0;

        // フラグ初期化
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
        RecoverAllDashCharge(true);
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
        RecoverAllDashCharge(true);
        playerColor = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAlive)
        {
            input.move = 0;
            input.jump = false;
            input.cancelJump = false;
            input.attack = false;
            input.dash = false;
        }

        // TOUCH SCREEN CONTROLS
#if UNITY_ANDROID
        input.move = Mathf.RoundToInt(touchMovement.GetTouchPosition.x);
        input.crouch = touchMovement.GetTouchPosition.y < -0.75f;
        input.attack = attackTouch.clicked;
        input.jump = jumpTouch.clicked;
        input.cancelJump = jumpTouch.released;
        input.use = itemTouch.clicked;
        input.cancelUse = itemTouch.released;
        input.dash = dashTouch.clicked;
#endif

        // プレイヤーのグラフィック部分を管理
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

            // プレイヤーの状態によって色を変更する
            hitTaintTimer -= Time.deltaTime;
            if (hitTaintTimer >= 0.0f) // ダメージくらった
            {
                playerColor = new Color(1.0f, 0.5f, 0.5f);
            }
            else if (((float)currentStamina / (float)maxStamina) < 0.2f) // スタミナが不足している
            {
                playerColor = new Color(0.3f, 0.3f, 0.7f);
            }
            else if (invulnerable) // 無敵状態
            {
                playerColor = Color.yellow;
            }
            else if (currentHP == 1) // 瀕死状態
            {
                playerColor = new Color(1.0f, 0.5f, 0.5f);
            }
            else // 普通
            {
                playerColor = Color.white;
            }
            graphic.DOColor(playerColor, 0.5f);

            // アニメーション再生
            animator.SetBool("IsJumping", rigidbody.velocity.y > 0.0f);
            animator.SetBool("IsFalling", rigidbody.velocity.y < 0.0f);
            animator.SetBool("MoveX", input.move != 0.0f);
        }

        // ジャンプを管理
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
                    StartJump(false, false);
                }
                else if (!jumpPressed && IsAlive())  // reset jump delay timer
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

        // reset input
        input.jump = false;
        input.cancelJump = false;
        
        // 攻撃処理
        // ATTACK
        {
            // 攻撃途中
            if (isAttacking && IsAlive())
            {
                // 今回の攻撃がダメージを与えているのか
                if (!alreadyDealDamage)
                {
                    attackDealDamageTimer += Time.deltaTime;

                    if ( attackDealDamageTimer >= attackDealTiming[(Mathf.Max((attackCombo-1), 0) % maxAttackComboPattern)] )
                    {
                        alreadyDealDamage = true;
                        CheckHitEnemy();
                    }
                }

                // 攻撃終了
                attackEndAttackTimer = Mathf.Max(attackEndAttackTimer - Time.deltaTime, 0.0f);

                // 当たり判定
                RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(dashDirection, 0.0f), collider.bounds.size.x, frameLayer);
                if (hit)
                {
                    transform.DOMoveX(transform.position.x, dashTime, false);
                }

                // ジャンプ中に攻撃するとしばらく空中に浮かせる
                rigidbody.velocity = new Vector2(0.0f, Mathf.Clamp(rigidbody.velocity.y, -0.00f, 0.00f));

                // 攻撃アニメーション終了
                if (!IsInAttackAnimation() && IsAlive())
                {
                    // アニメーション途中で攻撃ボタンが押されたら即次の攻撃開始
                    isAttacking = false;
                    if (attackPressed && !attackDisabled)
                    {
                        StartAttack();
                    }
                }

                // 攻撃ボタンが押されていた
                if (attackPressed)
                {
                    attackPressMemoryDelay = Mathf.Clamp(attackPressMemoryDelay - Time.deltaTime, 0.0f, attackPressMemoryTime);
                    if (attackPressMemoryDelay == 0.0f)
                    {
                        attackPressed = false;
                    }
                }

                // 前の攻撃アニメーションはすでに終わっていた
                if (attackEndAttackTimer == 0.0f)
                {
                    // 攻撃を許可する
                    if (input.attack)
                    {
                        StartAttack();
                    }
                }
            }

            // コンボ数をリセットする
            if (attackCombo > 0)
            {
                comboTimer = Mathf.Clamp(comboTimer - Time.deltaTime, 0.0f, comboTimeLast);
                if (comboTimer == 0.0f)
                {
                    // コンボをリセット
                    attackCombo = 0;
                }
            }

            // 攻撃ボタンが検出された
            if (input.attack)
            {
                if (!isAttacking && !attackDisabled)
                {
                    StartAttack();
                }
                else if (IsAlive())
                {
                    // 既に攻撃中
                    attackPressed = true;
                    attackPressMemoryDelay = attackPressMemoryTime;
                }

                // 入力をリセット
                input.attack = false;
            }
        }

        // ダッシュ関連
        // DASH
        {
            dashPressMemoryDelay = Mathf.Max(dashPressMemoryDelay - Time.deltaTime, 0.0f);

            if (input.dash || dashPressMemoryDelay > 0.0f)
            {
                if ((!isAttacking || attackEndAttackTimer == 0.0f) && dashCooldownTimer == 0.0f && currentDashCharge > 0)
                {
                    StartDash();
                }
                else if (input.dash)
                {
                    dashPressMemoryDelay = dashPressMemoryTime;
                }
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
                    if (deflect) CheckDeflectEnemy();

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

            // reset input
            input.dash = false;
        }

        //　しゃがみ
        // CROUCH
        {
            if (input.crouch && input.move == 0)
            {
                if (!IsJumping() && !IsDashing() && !IsAttacking() && !IsUsingPotion() && IsAlive())
                {
                    animator.SetBool("Crouch", true);
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch"))
                    {
                        staminaRegenDelayCounter = 0.0f;    // reset stamina regen delay
                        animator.Play("Crouch");
                    }
                }
            }
            else
            {
                animator.SetBool("Crouch", false);
            }
        }

        // 回復薬使用
        // USE POTION
        if (gameMng.IsPotionSelected())
        {
            if (input.use)
            {
                if (!IsJumping() && !IsDashing() && !IsAttacking() && IsAlive() && !input.crouch && input.move == 0 && !isPotionUsed)
                {
                    // START USING POTION
                    isUsingPotion = true;
                    animator.SetBool("Use", true);
                    animator.Play("Use");
                    potionProgressBarFill.size = new Vector2(0.0f, potionProgressBarFill.size.y);
                    potionProgressBar.gameObject.SetActive(true);
                    usingPotionTime = 0.0f;
                    AudioManager.Instance.PlaySFX("potionUsing");

                    chargeEffectInstantiated = Instantiate(chargeEffect, new Vector2(transform.position.x, transform.position.y + 0.1f), Quaternion.identity);
                    chargeEffectInstantiated.transform.SetParent(world);
                }
            }
            else if (input.cancelUse && IsUsingPotion())
            {
                CancelUsingPotion();
            }

            // LOOP
            if (IsUsingPotion())
            {
                if (IsJumping() || IsDashing() || IsAttacking() || input.crouch || input.move != 0)
                {
                    CancelUsingPotion();
                }

                // PROGRESSING
                usingPotionTime += Time.deltaTime;
                potionProgressBarFill.size = new Vector2(Mathf.Lerp(0.0f, 1.846f, usingPotionTime / potionSpeed), potionProgressBarFill.size.y);

                // SUCCESS
                if (usingPotionTime >= potionSpeed)
                {
                    CancelUsingPotion(true);
                    Regenerate((int)((((float)(potionHeal))/100f) * GetMaxHP()));
                    gameMng.SetItemCooldownAmount(1.0f);
                    isPotionUsed = true;
                    AudioManager.Instance.PlaySFX("potionHeal", 1.25f);
                    AudioManager.Instance.PlaySFX("Heal", 1.25f);
                    GameObject tmp = Instantiate(potionHealEffect, new Vector2(transform.position.x, -1.46f), Quaternion.identity);
                    tmp.transform.SetParent(world);
                }
            }

            // reset input
            input.use = false;
            input.cancelUse = false;
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
        //　キャラ移動制御
        // MOVE
        {
            float calculatedMoveSpeed = moveSpeed;
            if (currentStamina <= maxStamina / 5f)
            {
                calculatedMoveSpeed = moveSpeed / 2f;
            }
            if (IsJumping()) calculatedMoveSpeed = Mathf.Clamp(calculatedMoveSpeed, 0f, 10f);
            if (windrunnerBuffTimer > 0.0f) calculatedMoveSpeed += 6;
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

        //　回復制御
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
            if (IsAttacking())     multiplier = 0.0f;
            if (IsDashing())       multiplier *= 0.5f;
            if (input.move != 0)   multiplier *= 0.8f;
            if (IsJumping())       multiplier *= 0.5f;
            if (battlecry && monsterNearby) multiplier = 1.5f;
            if (staminaRegenDelayCounter > 0.0f) multiplier = 0.0f;

            staminaRegenDelayCounter = Mathf.Max(staminaRegenDelayCounter - Time.deltaTime, 0.0f);
            staminaRegenTimer = Mathf.Clamp(staminaRegenTimer - (Time.deltaTime * multiplier), 0.0f, staminaRegenInterval);
            if (staminaRegenTimer == 0.0f)
            {
                staminaRegenTimer = staminaRegenInterval;
                Regenerate(0, staminaRegen);
            }
            staminaRegenSlowed = (multiplier <= 0.2f);

            // DASH CHARGE
            if (dashRechargeTimer > 0.0f)
            {
                dashRechargeTimer = Mathf.Max(dashRechargeTimer - Time.deltaTime, 0.0f);
                if (dashRechargeTimer == 0.0f)
                {
                    gameMng.RecoverDashCharge(1, false); // UI
                    if (currentDashCharge < maxDashCharge) dashRechargeTimer = dashRechargeTime; // charge again
                }
            }
        }

        // WINDRUNNER
        {
            windrunnerBuffTimer = Mathf.Clamp(windrunnerBuffTimer - Time.deltaTime, 0.0f, windrunner);
        }
    }

    /// <summary>
    /// Android用入力
    /// </summary>
    public void TouchInput(Vector3 _input)
    {
        input.move = Mathf.RoundToInt(Mathf.Clamp(_input.x + 5.7f, -1,1));
    }

    /// <summary>
    /// ジャンプ開始
    /// </summary>
    public void StartJump(bool reviveJump = false, bool forcedJump = false)
    {
        // set flag
        jumpCancelled = false;
        jumpPressed = false;
        isJumping = true;
        isJumpCancellable = true;
        attackCombo = 0;

        // leave collision
        rigidbody.velocity = new Vector2(0f, 0.1f);

        // FX
        if (!reviveJump) Instantiate(jumpDustEffect, new Vector2(transform.position.x, -2.619f), Quaternion.identity).transform.SetParent(world);

        // determine which type of jump this is
        if (reviveJump) // this is a revive jump
        {
            rigidbody.AddForce(new Vector2(0.0f, jumpForce * 1.2f));
            isJumpCancellable = false;  // revive jump is forced
        }
        else if (forcedJump)
        {
            rigidbody.AddForce(new Vector2(0.0f, jumpForce * 0.8f));
            isJumpCancellable = false;  // forced jump is not cancellable
        }
        else if (!input.jump)   // this is a delayed jump
        {
            jumpCancelled = true;
            rigidbody.AddForce(new Vector2(0.0f, jumpForce / 2f));
        }
        else if ((float)currentStamina / (float)maxStamina < 0.2f)   // no stamina
        {
            rigidbody.AddForce(new Vector2(0.0f, jumpForce / 1.4f));
        }
        else // standard jump
        {
            rigidbody.AddForce(new Vector2(0.0f, jumpForce));
        }

        // SE
        AudioManager.Instance.PlaySFX("jump");

        gameMng.PlayerJumped();
    }

    /// <summary>
    /// キャラを強制ジャンプさせる
    /// </summary>
    public void ForceJump(float velocityForce = 0.4f)
    {
        // set flag
        jumpCancelled = false;
        jumpPressed = false;
        isJumping = true;
        isJumpCancellable = false;
        attackCombo = 0;
        
        // leave collision
        rigidbody.velocity = new Vector2(0f, 0.1f);
        rigidbody.AddForce(new Vector2(0.0f, jumpForce * velocityForce));
        gameMng.PlayerJumped();

    }

    /// <summary>
    /// ジャンプ途中でボタンが離された
    /// </summary>
    void CancelJump()
    {
        if (isJumpCancellable)
        {
            jumpCancelled = true;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, rigidbody.velocity.y / 2.0f);
        }
    }

    /// <summary>
    /// 攻撃開始
    /// </summary>
    void StartAttack()
    {
        bool costStamina = true;
        bool dashAttack = false;
        islightningLashAttack = false;

        // dash attack
        if (IsDashing())
        {
            dashAttack = true;
            //transform.DOKill(false);
            dashDirection = graphic.flipX ? -1 : 1;
            EndDash();
            attackCombo = 2;
            if (lightningLash > 0.0f && !IsJumping())
            {
                // talent bonus
                islightningLashAttack = true;
            }
        }

        // STAMINA RELATED
        if (currentStamina == 0)
        {
            // Need atleast 1 stamina point to attack
            if (!(juggernaut > 0 && attackCombo > juggernaut))
            {
                return;
            }
        }

        int calculatedStaminaCost = GetAttackDamage() + GetMaxDamage() + staminaCostAttackBase;
        if (windrunnerBuffTimer > 0.0f) calculatedStaminaCost /= 2;
        if (currentStamina < calculatedStaminaCost && costStamina)
        {
            staminaRegenDelayCounter = staminaRegenDelay * 2.5f;
        }

        // this attack cost no stamina
        if (juggernaut > 0 && attackCombo > juggernaut)
        {
            costStamina = false;
        }

        if (costStamina)
        {
            Regenerate(0, -calculatedStaminaCost);

            if (currentStamina == 0)
            {
                // reset combo if stamina is all gone
                attackCombo = 0;
            }
        }

        staminaRegenDelayCounter = staminaRegenDelay;

        // ANIMATION RELATED
        alreadyDealDamage = false;
        attackEndAttackTimer = FindAnimation(animator, "Attack" + (attackCombo % 3).ToString()).length * attackEndTiming[(attackCombo % 3)];
        attackDealDamageTimer = 0.0f;
        attackPressed = false;
        isAttacking = true;
        animator.Play("Attack" + (attackCombo % 3).ToString());
        comboTimer = comboTimeLast;

        if (dashAttack)
        {
            attackEndAttackTimer = attackEndAttackTimer * 0.75f;
            animator.Play("Attack" + (attackCombo % 3).ToString(), 0, 0.25f);
            attackDealDamageTimer = attackDealTiming[(attackCombo % 3)] * 0.5f;

            if (islightningLashAttack)
            {
                GameObject tmp = Instantiate(lightningLashEffect, new Vector2(transform.position.x + (dashDirection * attackRange), -1.75f), Quaternion.identity);
                tmp.transform.SetParent(transform);
                tmp.GetComponent<SpriteRenderer>().flipX = graphic.flipX;
                AudioManager.Instance.PlaySFX("thunder");
            }
        }

        // KNOCKBACK RELATED
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

        // chance to trigger echo ability
        const float chanceForEcho = 0.20f;
        if (isAttacking
            && echo > 0.0f 
            && Random.Range(0.0f, 1.0f) < chanceForEcho
            )
        {
            AudioManager.Instance.PlaySFX("echo", 0.75f);
            float illusionDelayTime = animator.GetCurrentAnimatorStateInfo(0).length / 2.0f;
            string animationName = "Attack" + (attackCombo % 3).ToString();
            StartCoroutine(EchoAttack(illusionDelayTime, animationName, IsFlip(), attackCombo));
        }


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

    /// <summary>
    /// 攻撃用敵の当たり判定
    /// </summary>
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
                if (isDash) calculatedAttackRange /= 1.5f;

                float enemyHitLeft = target.position.x + (enemy.GetCollider().bounds.size.x / 2f * -direction);
                float enemyHitRight = target.position.x + (enemy.GetCollider().bounds.size.x / 2f * direction);

                // デバッグ用
                Debug.DrawLine(new Vector2(playerAttackOriginPoint.x, playerAttackOriginPoint.y), new Vector2(enemyHitLeft, target.position.y), Color.red, 0.5f);
                Debug.DrawLine(new Vector2(playerAttackOriginPoint.x, playerAttackOriginPoint.y), new Vector2(enemyHitRight, target.position.y), Color.red, 0.5f);

                if ((Mathf.Abs(enemyHitLeft - playerAttackOriginPoint.x) < calculatedAttackRange || Mathf.Abs(enemyHitRight - playerAttackOriginPoint.x) < calculatedAttackRange)
                    && ((Mathf.Abs(target.position.y - playerAttackOriginPoint.y) < calculatedAttackRange * 0.88f )
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
                    isCriticalHit = enemy.CheckIfCriticalHit(playerAttackOriginPoint) || islightningLashAttack;
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

                    // check defend and armor
                    isDefended = enemy.CheckIfDefended(playerAttackOriginPoint) || enemy.CheckIfHaveArmor(calculatedDamage, playerAttackOriginPoint);
                    if (isDefended)
                    {
                        AudioManager.Instance.PlaySFX("defend" + Random.Range(0, 4).ToString(), 0.8f);
                        enemy.DefendDamage();
                        continue;
                    }

                    // apply damage
                    bool kill = enemy.DealDamage(calculatedDamage);

                    // calculate lightninglash lifesteal
                    int regenerate = 0;
                    if (lightningLash > 0.0f && islightningLashAttack)
                    {
                        regenerate += (int)(((float)calculatedDamage) * lightningLash);
                    }

                    // calculate berserker lifesteal
                    if (((float)currentHP / (float)maxHP) < 0.25f && berserker > 0)
                    {
                        regenerate += berserker;

                        GameObject tmp = Instantiate(bloodEffect, Vector2.Lerp(transform.position, enemy.transform.position, Random.Range(0.6f, 0.9f)), Quaternion.identity);
                        tmp.transform.SetParent(world);
                    }

                    // calculate lifedrain
                    if (lifedrain > 0 && kill)
                    {
                        regenerate += lifedrain;
                    }

                    // add extra timer
                    if (kill)
                    {
                        gameMng.TimerAddTime(5);
                    }

                    // apply lifesteals
                    Regenerate(regenerate);

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

    bool IsInAttackAnimation()
    {
        return (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack0")
             || animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1")
             || animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2"));
    }

    void CheckDeflectEnemy()
    {
        if (deflectSucceed) return;

        List<EnemyControl> enemyList = gameMng.GetMonsterList();

        if (enemyList.Count > 0)
        {
            foreach (EnemyControl enemy in enemyList)
            {
                if (deflectSucceed) continue;
                if (enemy.GetCollider().bounds.Contains(transform.position)
                    && enemy.GetCurrentStatus() == Status.Attacking
                    && enemy.IsAlive())
                {
                    deflectSucceed = true;
                }
            }
        }

        if (deflectSucceed)
        {
            // STAMINA
            gameMng.SpawnFloatingText(new Vector2(transform.position.x, transform.position.y + collider.bounds.size.y / 2f), 2f, 25f,
                                     (Mathf.FloorToInt((GetStaminaMax() - currentStamina) / 2.0f)).ToString(), Color.blue, new Vector2(0, 1), 80f);

            Regenerate(0, (GetStaminaMax() - currentStamina) / 2);

            AudioManager.Instance.PlaySFX("deflect");
            AudioManager.Instance.PlaySFX("comboMaster");
        }
    }

    void CancelUsingPotion(bool success = false)
    {
        isUsingPotion = false;
        animator.SetBool("Use", false);
        potionProgressBarFill.size = new Vector2(0.0f, potionProgressBarFill.size.y);
        potionProgressBar.gameObject.SetActive(false);

        if (chargeEffectInstantiated != null)
        {
            Destroy(chargeEffectInstantiated);
        }

        if (!success)
        {
            AudioManager.Instance.PlaySFX("potionCancel");
        }
    }

    void StartDash()
    {
        dashDamagedEnemy.Clear();
        deflectSucceed = false;
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        animator.Play("Dash");

        // Calculate dash charge first
        if (currentDashCharge == maxDashCharge) dashRechargeTimer = dashRechargeTime; // first charge used
        gameMng.UseDashCharge(); // UI

        // if no key is pressed dash into facing direction
        if (input.move != 0)
        {
            dashDirection = input.move;
            // force character facing direction into dash direction
            graphic.flipX = dashDirection != 1;
        }
        else
        {
            dashDirection = graphic.flipX ? -1 : 1;
        }

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
        staminaRegenDelayCounter = staminaRegenDelay;
    }

    private void EndDash()
    {
        isDashing = false;

        graphic.sortingLayerName = "Player";

        // windrunner effect
        if (windrunner > 0.0f)
        {
            windrunnerBuffTimer = windrunner;
            AfterEffectDuration(afterEffectInterval /2f, windrunner);
        }
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
    public int GetCurrentDashCharge()
    {
        return currentDashCharge;
    }
    public int GetMaxDashCharge()
    {
        return maxDashCharge;
    }
    public void SetCurrentDashCharge(int value)
    {
        currentDashCharge = value;
    }

    public void Regenerate(int hp, int stamina = 0, bool showFloatingText = true)
    {
        currentHP = Mathf.Clamp(currentHP + hp, 0, maxHP);
        currentStamina = Mathf.Clamp(currentStamina + stamina, 0, maxStamina);

        if (hp > 0 && showFloatingText)
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

    public void StopStaminaRegenerate()
    {
        staminaRegenDelayCounter = staminaRegenDelay;
    }
    public void DisableDash(float time)
    {
        dashCooldownTimer = time;
    }

    public void RecoverAllDashCharge(bool instant)
    {
        gameMng.RecoverAllDashCharge(instant);
    }

    public bool DealDamage(int value, Transform source)
    {
        if (!isAlive) return false;
        if (isDashing) return false;
        if (value <= 0) return false;

        if (invulnerable)
        {
            AudioManager.Instance.PlaySFX("defend2");
            Instantiate(holyShieldEffect, Vector2.MoveTowards(transform.position, source.position, 0.5f), Quaternion.identity);
            return false;
        }

        // Break-fall effect
        if (breakfallCost > 0 && currentStamina >= breakfallCost && animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch"))
        {
            Regenerate(0, -breakfallCost);

            AudioManager.Instance.PlaySFX("defend3");
            Instantiate(shieldEffect, Vector2.MoveTowards(transform.position, source.position, 0.5f), Quaternion.identity);
            staminaRegenDelayCounter = staminaRegenDelay;
            return false;
        }

        gameMng.ResetCombo(currentCombo);
        currentCombo = -1;

        int originalHp = currentHP;

        // deathblow system. Sometimes it will give give 1 more chance (1 hp left)
        if (((float)currentHP / (float)maxHP) > 0.1f && Random.Range(0.0f, 1.0f) < 0.2f)
        {
            currentHP = Mathf.Clamp(currentHP - value, 1, maxHP);
        }
        else
        {
            // last hit
            currentHP = Mathf.Clamp(currentHP - value, 0, maxHP);
        }

        // calculate damage dealt
        int damageDealt = originalHp - currentHP;

        // check direction from source of damage
        int direction = source.position.x > transform.position.x ? -1 : 1;

        // generate floating text if more than 1 damage is dealt
        if (damageDealt > 0)
        {
            const float floatSize = 100;
            Vector2 randomizeOffset = new Vector2(Random.Range(-collider.bounds.size.x / 2f, collider.bounds.size.x / 2f), Random.Range(-0.5f, 0.5f));
            gameMng.SpawnFloatingText(new Vector2(transform.position.x, transform.position.y) + randomizeOffset
                                         , 4f + Random.Range(1.0f, 3.0f), 25f + Random.Range(0.0f, 25.0f),
                                         damageDealt.ToString(), new Color(0.75f, 0.0f, 0.0f), new Vector2(direction * 1f, 1f), floatSize);
        }

        // Shake HP Bar if more than 1 damage is dealt
        if (damageDealt > 0)
        {
            // shake 2 time minimal
            gameMng.ShakeHPBar(Mathf.Max(damageDealt / 10, 2));
        }

        // color taint effect
        hitTaintTimer = hitTaintTime;

        if (IsUsingPotion())
        {
            CancelUsingPotion();
        }

        if (input.move == 0 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch"))
        {
            animator.Play("Hurt");
        }

        Knockback(((float)value / (float)Mathf.Max(maxHP, 1)) * 10f, direction, 0.1f);

        // check if player is dead after this attack.
        if (currentHP == 0)
        {
            // dead
            graphic.flipX = direction == 1;
            animator.Play("Dead");
            isAlive = false;
            AudioManager.Instance.PlaySFX("criticalplayerHit", 0.8f);
            AudioManager.Instance.PlaySFX("Body Fall", 1f);
            StartCoroutine(Dead(FindAnimation(animator, "Dead").length));
            return true;
        }

        // play audio
        AudioManager.Instance.PlaySFX("playerHit", 0.8f);
        return true;
    }

    /// <summary>
    /// 死亡アニメーション
    /// </summary>
    IEnumerator Dead(float waitTime)
    {
        yield return new WaitForSeconds(waitTime/2f);
        AudioManager.Instance.PlaySFX("dead", 0.85f);
        yield return new WaitForSeconds(waitTime/2f);

        // 復活できるのか
        if (survivor)
        {
            // revive
            Instantiate(reviveParticle, new Vector2(transform.position.x, -1.14f), Quaternion.identity);
            Instantiate(holySwordEffect, new Vector2(transform.position.x, 2.0f), Quaternion.identity);
            Instantiate(holySwordEffect, new Vector2(transform.position.x - 2.0f, 1.0f), Quaternion.identity);
            Instantiate(holySwordEffect, new Vector2(transform.position.x + 2.0f, 1.0f), Quaternion.identity);
            AudioManager.Instance.PlaySFX("revive");
            animator.Play("Jump");
            isAlive = true;
            survivor = false;

            // make sure player have full stamina before jump
            currentHP = maxHP;
            currentStamina = maxStamina;
            RecoverAllDashCharge(false);

            StartJump(true, false);

            AfterEffectDuration(afterEffectInterval, 0.75f);
            gameMng.SpawnFloatingText(new Vector2(transform.position.x, transform.position.y + collider.bounds.size.y / 2f), 5f, 30f,
                                     "RESURRECTION", new Color(1f, 8.43f, 0.0f), new Vector2(0f, 1f), 100f);

            graphic.color = Color.yellow;
            StartCoroutine(InvulnerableForSeconds(3.0f));
        }
        else
        {
            // ゲーム終了
            // end game
            yield return new WaitForSeconds(0.5f);
            gameMng.GameOver();
        }
    }

    /// <summary>
    /// 特殊スキール：エコー
    /// </summary>
    IEnumerator EchoAttack(float delayTime, string animationName, bool isflip, int combo)
    {
        yield return new WaitForSeconds(delayTime);

        // create illusion
        //--- spawning new empty object, copying tranform ---
        GameObject afterImg = new GameObject("echo illusion");
        afterImg.transform.position = graphic.transform.position;
        afterImg.transform.rotation = graphic.transform.rotation;
        afterImg.transform.localScale = graphic.transform.localScale;
        afterImg.gameObject.layer = 0;
        //--- copying spriterenderer ---
        SpriteRenderer echoRenderer = afterImg.AddComponent<SpriteRenderer>();
        SpriteRenderer originalRenderer = graphic.GetComponent<SpriteRenderer>();
        echoRenderer.sortingOrder = originalRenderer.sortingOrder - 1;
        echoRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        echoRenderer.sprite = originalRenderer.sprite;
        echoRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        echoRenderer.flipX = isflip;
        echoRenderer.material = originalRenderer.material;
        //--- copying animator ---
        Animator copiedAnimator = afterImg.AddComponent<Animator>();
        copiedAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
        copiedAnimator.Play(animationName);
        //--- move forward ---
        int direction = isflip ? -1 : 1;
        float illusionTime = copiedAnimator.GetCurrentAnimatorClipInfo(0).Length * 0.3f;
        afterImg.transform.DOMoveX((direction * 1.5f) + afterImg.transform.position.x, illusionTime);
        //--- done ---
        Destroy(afterImg, illusionTime);

        yield return new WaitForSeconds(attackDealTiming[(Mathf.Max((combo), 0) % 3)]);

        List<EnemyControl> enemyList = gameMng.GetMonsterList();

        Vector2 playerAttackOriginPoint = new Vector2(afterImg.transform.position.x + (attackRange * direction), afterImg.transform.position.y);

        bool isHitEnemy = false;
        bool isDefended = false;
        if (enemyList.Count > 0)
        {
            foreach (EnemyControl enemy in enemyList)
            {
                Transform target = enemy.transform;
                float calculatedAttackRange = attackRange;

                float enemyHitLeft = target.position.x + (enemy.GetCollider().bounds.size.x / 2f * -direction);
                float enemyHitRight = target.position.x + (enemy.GetCollider().bounds.size.x / 2f * direction);
                Debug.DrawLine(new Vector2(playerAttackOriginPoint.x, playerAttackOriginPoint.y), new Vector2(enemyHitLeft, target.position.y), Color.red, 0.5f);
                Debug.DrawLine(new Vector2(playerAttackOriginPoint.x, playerAttackOriginPoint.y), new Vector2(enemyHitRight, target.position.y), Color.red, 0.5f);
                if ((Mathf.Abs(enemyHitLeft - playerAttackOriginPoint.x) < calculatedAttackRange || Mathf.Abs(enemyHitRight - playerAttackOriginPoint.x) < calculatedAttackRange)
                    && ((Mathf.Abs(target.position.y - playerAttackOriginPoint.y) < calculatedAttackRange * 0.88f)
                    || (Mathf.Abs((target.position.y + enemy.GetHeight() / 2f) - playerAttackOriginPoint.y) < calculatedAttackRange * 0.75f))
                    && enemy.IsAlive()
                    && !enemy.IsInvulnerable())
                {
                    // calculate damage
                    int calculatedDamage = (int)((float)(baseDamage+maxDamage) * echo);

                    // check is third attack
                    calculatedDamage = attackCombo % 3 == 0 ? (int)((float)calculatedDamage * 1.5f) : calculatedDamage;

                    // check defend and armor
                    isDefended = enemy.CheckIfDefended(playerAttackOriginPoint) || enemy.CheckIfHaveArmor(calculatedDamage, playerAttackOriginPoint);
                    if (isDefended)
                    {
                        AudioManager.Instance.PlaySFX("defend" + Random.Range(0, 4).ToString(), 0.8f);
                        enemy.DefendDamage();
                        continue;
                    }

                    // apply damage
                    bool kill = enemy.DealDamage(calculatedDamage);

                    // calculate lifedrain
                    if (lifedrain > 0 && kill)
                    {
                        Regenerate(lifedrain);
                    }

                    // add extra timer
                    if (kill)
                    {
                        gameMng.TimerAddTime(5);
                    }

                    // calculate knockback distance
                    float baseDistance = attackMoveRange[(Mathf.Max((attackCombo - 1), 0) % 3)];
                    float multiplier = 1.0f * ((Mathf.Abs(target.position.x - transform.position.x) / 4f) / (baseDistance));
                    enemy.Knockback(baseDistance * multiplier, direction, 0.15f);

                    isHitEnemy = true;

                    // create particle effect
                    GameObject particle = Instantiate(hitParticleEffect, Vector2.Lerp(transform.position, enemy.transform.position, 0.5f), Quaternion.identity);
                    particle.GetComponent<ParticleScript>().SetParticleColor(gameMng.GetThemeColor());
                    particle.transform.SetParent(world, true);
                    particle = Instantiate(hitFXEffect, Vector2.Lerp(transform.position, enemy.transform.position, Random.RandomRange(0.6f, 0.9f)), Quaternion.identity);
                    particle.transform.eulerAngles = new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-180f, 180f));
                    particle.transform.SetParent(world, true);

                    // create floating text
                    Color fontColor = Color.white;
                    float floatSize = Mathf.Clamp(50 * (calculatedDamage / baseDamage), 0, 80);
                    string extraText = string.Empty;

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
            AudioManager.Instance.PlaySFX("hit");
            
            currentCombo++;
            comboResetTimer = comboResetTime;
            gameMng.SetCombo(currentCombo);
        }
    }

    /// <summary>
    /// 無敵時間
    /// </summary>
    IEnumerator InvulnerableForSeconds(float time)
    {
        invulnerable = true;

        yield return new WaitForSeconds(time);

        invulnerable = false;
    }

    /// <summary>
    /// 無敵時間
    /// </summary>
    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    /// <summary>
    /// 撃退
    /// </summary>
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

    /// <summary>
    /// キャラクターがすごい勢いで地面に当たる
    /// </summary>
    void SuperLanding()
    {
        gameMng.ScreenImpactGround(0.04f, 0.4f);
        gameMng.ScreenChangeTheme();
        gameMng.SetItemCooldownAmount(0.0f);
        gameMng.TimerAddTimeNewLevel();
        ResetPotionUse();
        GameObject tmp = Instantiate(impactParticleEffect, Vector2.Lerp(transform.position, new Vector2(transform.position.x, transform.position.y - collider.bounds.size.y /2f),0.5f)
            , Quaternion.identity);
        tmp.GetComponent<ParticleScript>().SetParticleColor(gameMng.GetThemeColor());
        AudioManager.Instance.PlaySFX("impact");
    }
    
    /// <summary>
    /// キャラクター強化
    /// </summary>
    public void ApplyBonus(Skill skill, float value)
    {
        switch (skill)
        {
            case Skill.MoveSpeed:
                moveSpeed += value;
                break;
            case Skill.BaseDamage:
                baseDamage += (ProtectedUInt16)value;
                break;
            case Skill.MaxDamage:
                maxDamage += (ProtectedUInt16)value;
                break;
            case Skill.Vitality:
                maxHP += (ProtectedUInt16)value;
                currentHP += (ProtectedUInt16)value;
                break;
            case Skill.DashCooldown:
                dashCooldown = dashCooldown - (dashCooldown * (value/100f));
                break;
            case Skill.DashDamage:
                dashDamage += (ProtectedUInt16)value;
                break;
            case Skill.Stamina:
                maxStamina += (ProtectedUInt16)value;
                currentStamina+= (ProtectedUInt16)value;
                break;
            case Skill.StaminaRecoverSpeed:
                staminaRegen += (ProtectedUInt16)value;
                break;
            case Skill.HPRegen:
                hpRegen += (ProtectedUInt16)value;
                break;
            case Skill.LightningLash:
                lightningLash += value/100f;
                break;
            case Skill.LifeDrain:
                lifedrain += (ProtectedUInt16)value;
                break;
            case Skill.Survivor:
                survivor = true;
                break;
            case Skill.ComboMaster:
                comboMaster += (ProtectedUInt16)value;
                break;
            case Skill.Windrunner:
                windrunner += value;
                break;
            case Skill.BreakFall:
                breakfallCost += (ProtectedUInt16)value;
                break;
            case Skill.Potion:
                potionHeal += (ProtectedUInt16)value;
                break;
            case Skill.Deflect:
                deflect = true;
                break;
            case Skill.Berserker:
                berserker += (ProtectedUInt16)value;
                break;
            case Skill.Battlecry:
                battlecry = true;
                break;
            case Skill.Echo:
                echo += value/100.0f;
                break;
            case Skill.Juggernaut:
                juggernaut = (ProtectedUInt16)value;
                break;
            default:
                Debug.Log("<color=red>skill data not found!</color>");
                break;
        }
    }

    public bool IsUsingPotion()
    {
        return isUsingPotion;
    }

    public void ResetPotionUse()
    {
        isPotionUsed = false;
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
    public float GetLightningLash()
    {
        return lightningLash;
    }
    public LayerMask GetFrameLayer()
    {
        return frameLayer;
    }

    public int GetLifeDrain()
    {
        return lifedrain;
    }
    public int GetComboMaster()
    {
        return comboMaster;
    }
    public bool GetIsBattlecry()
    {
        return battlecry;
    }
    public float GetWindrunner()
    {
        return windrunner;
    }
    public int GetBreakFallCost()
    {
        return breakfallCost;
    }
    public bool GetDeflect()
    {
        return deflect;
    }
    public int GetPotionHeal()
    {
        return potionHeal;
    }
    public float GetDashCD()
    {
        return dashCooldown;
    }
    public bool GetSurvivor()
    {
        return survivor;
    }
    public float GetEcho()
    {
        return echo;
    }
    public int GetJuggernaut()
    {
        return juggernaut;
    }
    public bool IsFlip()
    {
        return graphic.flipX;
    }


    public float GetCurrentInputMove()
    {
        return input.move;
    }
}
