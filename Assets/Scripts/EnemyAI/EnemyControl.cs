using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyControl : MonoBehaviour
{
    [Header("Custom Parameter")]
    [SerializeField] private string enemyName = "JellySlime";
    [SerializeField] private bool isBoss = false;
    [SerializeField] private int maxHp = 20;
    [SerializeField] private float startHpPercentage = 1.0f;
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int maxArmor = 20;
    [SerializeField] private bool fadeOutAfterDead = false;
    [SerializeField] private bool attackedStopAttack = true;
    [SerializeField, Range(0.1f, 3.0f)] private float mass = 1.0f;
    [SerializeField] private string steamAchievementCode = "";

    [Header("Special")]
    [SerializeField] private bool critialHitOnly = false;
    [SerializeField] private bool backSideOnly = false;
    [SerializeField] private bool alwaysIgnoreKnockback = false;
    [SerializeField] private bool haveArmor = false;

    [Header("CriticalPart")]
    [SerializeField] private bool enableWeaknessPoint = false;
    [SerializeField] private Vector2 weaknessPointPivot;
    [SerializeField] private float weaknessPointRange;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private SpriteRenderer hpbar;
    [SerializeField] private SpriteRenderer hpbarFade;
    [SerializeField] private SpriteRenderer armorbar;
    [SerializeField] private SpriteRenderer armorFade;
    [SerializeField] private SpriteRenderer graphic;
    [SerializeField] private LayerMask frameLayer;

    [Header("Debug")]
    [SerializeField] private Status currentStatus;
    [SerializeField] private int currentHp;
    [SerializeField] private int currentStamina;
    [SerializeField] private int currentArmor;
    [SerializeField] private bool immumeKnockback = false;
    [SerializeField] private bool statusChanged = false;
    [SerializeField] private bool criticalHitted = false;
    [SerializeField] private bool invulnerable = false;

    private Collider2D collider;
    private Rigidbody2D rigidbody;
    private bool isAlive;
    private float originalHpBarScale;
    private float originalArmorBarScale;
    GameManager gameMng;
    private bool superland;
    private float afterImgInterval = 0.2f, afterImgCnt;
    private int level;
    private bool isknockbacking;
    private bool isPaused;
    private GameObject shieldEffect;
    private Controller player;
    private bool isInitialized = false;
    private Color originalColor = Color.white;

    private void Awake()
    {
        shieldEffect = Resources.Load("Prefabs/Shield") as GameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        Initialization();
    }

    public void Initialization()
    {
        if (isInitialized) return; // avoid double init
        isInitialized = true;
        player = FindObjectOfType<Controller>();
        collider = GetComponent<Collider2D>();
        rigidbody = GetComponent<Rigidbody2D>();
        currentHp = Mathf.FloorToInt((float)maxHp * startHpPercentage);
        currentArmor = maxArmor;
        isAlive = true;
        criticalHitted = false;
        gameMng = Object.FindObjectOfType<GameManager>();

        if (hpbar != null)
        {
            if (!isBoss)
            {
                originalHpBarScale = hpbar.transform.localScale.x;
                hpbar.DOFade(0.0f, 0.0f);
                hpbarFade.DOFade(0.0f, 0.0f);

                // rescale
                hpbar.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 0.0f);
                hpbarFade.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 0.0f);
            }
            else
            {
                // boss unit have his dedicated hp bar so hide this local hp bar.
                hpbar.gameObject.SetActive(false);
                hpbarFade.gameObject.SetActive(false);
            }
        }

        if (armorbar != null)
        {
            originalArmorBarScale = armorbar.transform.localScale.x;
            armorbar.DOFade(0.0f, 0.0f);
            armorFade.DOFade(0.0f, 0.0f);
        }

        originalColor = graphic.color;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // collide with player
        if (isAlive)
        {
            Collider2D[] colliders = new Collider2D[1];
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(playerLayer);
            int colliderCount = collider.OverlapCollider(contactFilter, colliders);
            if (colliderCount > 0)
            {
                Controller player = colliders[0].GetComponent<Controller>();

                if (Vector2.Distance(player.transform.position, transform.position) < collider.bounds.size.x * 0.5f
                    && !player.IsDashing()
                    && !player.IsJumping()
                    && !player.IsAttacking()
                    && player.IsAlive()
                    && !isknockbacking
                    && !immumeKnockback
                    && !alwaysIgnoreKnockback)
                {
                    float direction = player.transform.position.x > transform.position.x ? -1.0f : 1.0f;
                    transform.DOMoveX(transform.position.x + direction * collider.bounds.size.x * 0.5f, 0.1f, false);
                }
            }
        }

        if (superland)
        {
            afterImgCnt -= Time.deltaTime;

            if (afterImgCnt <= 0.0f)
            {
                afterImgCnt = afterImgInterval;
                CreateAfterImage();
            }

            if (GetComponent<Rigidbody2D>().velocity.y == 0.0f)
            {
                superland = false;
                SuperLanding();
            }
        }
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    public void DefendDamage()
    {
        graphic.DOColor(new Color(Color.blue.r, Color.blue.g, Color.blue.b, originalColor.a), 0.0f);
        graphic.DOColor(originalColor, 0.5f);
    }

    public bool DealDamage(int value)
    {
        bool rtn = false;
        currentHp = Mathf.Clamp(currentHp - value, 0, maxHp);

        if (!isBoss)
        {
            // rescale hp bar
            hpbar.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 0.0f);
            hpbarFade.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 1f);

            // show and fade
            hpbar.DOFade(1.0f, 0.0f);
            hpbarFade.DOFade(1.0f, 0.0f);

            hpbar.DOFade(0.0f, 2f);
            hpbarFade.DOFade(0.0f, 2f);
        }

        if (currentHp == 0)
        {
            isAlive = false;
            animator.Play(enemyName + "Dead");

            if (fadeOutAfterDead)
            {
                StartCoroutine(FadeOutWithDelay(0.0f, FindAnimation(animator, enemyName + "Dead").length, 2.0f));
            }
            else
            {
                StartCoroutine(FadeOutWithDelay(0.0f, FindAnimation(animator, enemyName + "Dead").length, 0.0f));
            }

            rigidbody.isKinematic = true;
            currentStatus = Status.Dying;
            statusChanged = true;
            rtn = true;

            // Result Manager Update
            ResultManager.Instance().CountMonsterKill();
            if (isBoss) ResultManager.Instance().CountBossKill();

            // SteamAchivement Unlock
            if (steamAchievementCode != string.Empty)
            {
                SteamworksNetManager.Instance().UnlockAchievement(steamAchievementCode);
            }
        }
        else
        {
            if (currentStatus != Status.Attacking || attackedStopAttack)
            {
                currentStatus = Status.Attacked;
                statusChanged = true;
                if (FindAnimation(animator, enemyName + "Hit") != null)
                {
                    animator.Play(enemyName + "Hit");
                }
            }
        }

        graphic.DOColor(new Color(Color.red.r, Color.red.g, Color.red.b, originalColor.a), 0.0f);
        graphic.DOColor(originalColor, 0.5f);

        return rtn;
    }


    public void Heal(int value)
    {
        currentHp = Mathf.Clamp(currentHp + value, 0, maxHp);

        if (!isBoss)
        {
            // rescale hp bar
            hpbar.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 1f);
            hpbarFade.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 0f);

            // show and fade
            hpbar.DOFade(1.0f, 0.0f);
            hpbarFade.DOFade(1.0f, 0.0f);

            hpbar.DOFade(0.0f, 2f);
            hpbarFade.DOFade(0.0f, 2f);
        }
    }

    public bool CheckIfCriticalHit(Vector2 damageSource)
    {
        if (!enableWeaknessPoint) return false;

        if (Vector2.Distance(damageSource, (Vector2)transform.position + weaknessPointPivot) < weaknessPointRange)
        {
            criticalHitted = true;
            return true;
        }

        return false;
    }

    public bool CheckIfDefended(Vector2 damageSource)
    {
        if (critialHitOnly)
        {
            if (!criticalHitted)
            {
                Instantiate(shieldEffect, Vector2.Lerp(damageSource, transform.position, 0.5f), Quaternion.identity);
                return true;
            }
        }

        if (backSideOnly)
        {
            if (!((damageSource.x > transform.position.x
                && graphic.flipX)
                ||
                (damageSource.x < transform.position.x
                && !graphic.flipX)))
            {
                Instantiate(shieldEffect, Vector2.Lerp(damageSource, transform.position, 0.5f), Quaternion.identity);
                return true;
            }
        }

        return false;
    }

    public bool CheckIfHaveArmor(int damage, Vector2 damageSource)
    {
        if (!haveArmor) return false;

        if (currentArmor > 0)
        {
            // reduce armor
            currentArmor = Mathf.Max(currentArmor - damage, 0);

            // effects
            GameObject tmp = Instantiate(shieldEffect, Vector2.Lerp(damageSource, transform.position, 0.5f), Quaternion.identity);
            tmp.transform.SetParent(transform.parent);

            // color tint
            graphic.DOColor(new Color(Color.blue.r, Color.blue.g, Color.blue.b, originalColor.a), 0.0f);
            graphic.DOColor(originalColor, 0.5f);

            // rescale armor bar
            if (!ReferenceEquals(armorbar, null) && !ReferenceEquals(armorFade, null))
            {
                armorbar.transform.DOScaleX(((float)currentArmor / (float)maxArmor) * originalArmorBarScale, 0.0f);
                armorFade.transform.DOScaleX(((float)currentArmor / (float)maxArmor) * originalArmorBarScale, 1f);

                // show and fade
                armorbar.DOFade(1.0f, 0.0f);
                armorFade.DOFade(1.0f, 0.0f);

                armorbar.DOFade(0.0f, 2f);
                armorFade.DOFade(0.0f, 2f);
            }
            return true;
        }

        return false;
    }

    IEnumerator FadeOutWithDelay(float targetValue, float delay, float fadeTime)
    {
        yield return new WaitForSeconds(delay);

        graphic.DOFade(0.0f, fadeTime);

        gameMng.MonsterDied(this);
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

    public void Knockback(float value, int direction, float time)
    {
        // face toward the source of knockback
        if (currentStatus != Status.Attacking)
        {
            graphic.flipX = direction == 1;
        }

        float calculatedValue = value / mass;

        // loop
        StartCoroutine(knockbackLoop(transform.position.x + calculatedValue * direction, time));
    }

    IEnumerator knockbackLoop(float targetX, float time)
    {
        float timeElapsed = 0.0f;
        isknockbacking = true;
        float originalPos = transform.position.x;
        while (timeElapsed < time && !immumeKnockback)
        {
            timeElapsed += Time.deltaTime;
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(Mathf.Lerp(originalPos, targetX, timeElapsed / time), 0.0f), collider.bounds.size.x, frameLayer);
            if (!hit)
            {
                transform.position = new Vector2(Mathf.Lerp(originalPos, targetX, timeElapsed / time), transform.position.y);
            }
            yield return null;
        }
        isknockbacking = false;
    }

    public Animator GetAnimator()
    { 
        return animator;
    }

    public SpriteRenderer GetGraphic()
    { 
        return graphic;
    }

    public LayerMask GetPlayerLayer()
    { 
        return playerLayer;
    }

    public string GetName()
    {
        return enemyName;
    }

    public Status GetCurrentStatus()
    {
        return currentStatus;
    }

    public void SetCurrentStatus(Status nextStatus)
    {
        currentStatus = nextStatus;
    }

    public bool IsStatusChanged()
    {
        return statusChanged;
    }

    public bool IsKnockbacking()
    {
        return isknockbacking;
    }

    public void ResetStatusChanged()
    {
        statusChanged = false;
    }

    public bool IsStaminaMax()
    {
        return (maxStamina == currentStamina);
    }

    public void UseAllStamina()
    {
        currentStamina = 0;
    }

    public void RegenStamina(int value)
    {
        currentStamina = Mathf.Clamp(currentStamina + value, 0, maxStamina);
    }

    public void SetImmumetoKnockback(bool boolean)
    {
        immumeKnockback = boolean;
    }
    public bool IsImmumetoKnockback()
    {
        return immumeKnockback;
    }

    public float GetHeight()
    {
        return collider.bounds.size.y;
    }

    public bool IsCriticalHitted()
    {
        return criticalHitted;
    }

    public bool IsAlwaysIgnoreKnockback()
    {
        return alwaysIgnoreKnockback;
    }

    public void ResetCriticalHitFlag()
    {
        criticalHitted = false;
    }

    public void SuperLand()
    {
        superland = true;
        afterImgCnt = afterImgInterval;
        GetComponent<Rigidbody2D>().velocity = new Vector2(0.0f, -60f);
    }

    public void SetLevel(int lvl)
    {
        level = lvl;
    }

    public void AddMaxHp(int value)
    {
        maxHp += value;
        currentHp += value;
    }

    public void SetCurrentHpDirectly(int value)
    {
        currentHp = value;
    }

    public int GetLevel()
    {
        return level;
    }

    void SuperLanding()
    {
        //gameMng.ScreenImpactGround(0.04f, 0.4f);
        //AudioManager.Instance.PlaySFX("impact", 0.7f);
    }

    public void CreateAfterImage(float time = 0.5f)
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
        Destroy(afterImg, time);

        // ask game manager to remove this object if the player restart the game.
        gameMng.RegisterExtraStuff(gameObject);
    }

    public void TurnTowardPlayer()
    {
        if (!IsFacingPlayer())
        {
            graphic.flipX = !graphic.flipX;
        }
    }
    
    // spawn special effect. Return reference
    public GameObject SpawnSpecialEffect(GameObject prefab, Vector2 pos, bool isParentThisEnemy)
    {
        GameObject tmp = Instantiate(prefab, pos, Quaternion.identity);
         if (isParentThisEnemy) tmp.transform.SetParent(transform);

        return tmp;
    }

    public void Pause(bool boolean)
    {
        isPaused = boolean;
        if (boolean)
        {
            enabled = false;
        }
        else
        {
            enabled = true;
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    public LayerMask GetFrameLayer()
    {
        return frameLayer;
    }

    public int GetCurrentHP()
    {
        return currentHp;
    }
    public int GetMaxHP()
    {
        return maxHp;
    }

    public float GetCurrentHPPercentage()
    {
        return (float)currentHp / (float)maxHp;
    }

    public void SetInvulnerable(bool boolean)
    {
        invulnerable = boolean;
    }

    public bool IsInvulnerable()
    {
        return invulnerable;
    }
    public bool IsBoss()
    {
        return isBoss;
    }


    public bool IsFacingPlayer()
    {
        return ((player.transform.position.x > transform.position.x && !graphic.flipX) || (player.transform.position.x < transform.position.x && graphic.flipX));
    }
    
    public bool IsInScreen()
    {
        return (transform.localPosition.x > -8.0f && transform.localPosition.x < 8.0f);
    }

    public int GetDirectionInteger()
    {
        return graphic.flipX ? -1 : 1;
    }

    public Collider2D GetCollider()
    {
        return collider;
    }

    public Rigidbody2D GetRigidBody()
    {
        return rigidbody;
    }

    public Controller GetPlayer()
    {
        return player;
    }

    public GameManager GetGameManager()
    {
        return gameMng;
    }

    public int GetCurrentArmor()
    {
        return currentArmor;
    }
    public int GetMaxArmor()
    {
        return maxArmor;
    }

    public void SetArmor(int value)
    {
        currentArmor = Mathf.Clamp(value, 0, maxArmor);
    }

    public void ShowArmorBar()
    {
        if (!ReferenceEquals(armorbar, null) && !ReferenceEquals(armorFade, null))
        {
            // rescale armor bar
            armorbar.transform.DOScaleX(((float)currentArmor / (float)maxArmor) * originalArmorBarScale, 0.5f);
            armorFade.transform.DOScaleX(((float)currentArmor / (float)maxArmor) * originalArmorBarScale, 0.5f);

            armorbar.DOFade(1.0f, 0.0f);
            armorbar.DOFade(0.0f, 2f);
        }
    }

    public void RegeneraeArmor(int value)
    {
        currentArmor = Mathf.Clamp(currentArmor + value, 0, maxArmor);
        ShowArmorBar();
        if (value > 0)
        {
            gameMng.SpawnFloatingText(new Vector2(transform.position.x, transform.position.y + collider.bounds.size.y / 2f), 2f, 25f,
                                        value.ToString(), new Color(0.3f, 0.3f, 1.0f), new Vector2(0, 1), 80f);
        }
    }

    public void SetIsBoss(bool value)
    {
        isBoss = value;
    }
}
