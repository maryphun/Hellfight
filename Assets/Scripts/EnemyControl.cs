using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyControl : MonoBehaviour
{
    [Header("Custom Parameter")]
    [SerializeField] private string enemyName = "JellySlime";
    [SerializeField] private int maxHp = 20;
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private bool fadeOutAfterDead = false;
    [SerializeField] private bool attackedStopAttack = true;
    [SerializeField, Range(0.1f, 3.0f)] private float mass = 1.0f;

    [Header("Special")]
    [SerializeField] private bool critialHitOnly = false;
    [SerializeField] private bool backSideOnly = false;
    [SerializeField] private bool alwaysIgnoreKnockback = false;

    [Header("CriticalPart")]
    [SerializeField] private bool enableWeaknessPoint = false;
    [SerializeField] private Vector2 weaknessPointPivot;
    [SerializeField] private float weaknessPointRange;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private SpriteRenderer hpbar;
    [SerializeField] private SpriteRenderer hpbarFade;
    [SerializeField] private SpriteRenderer graphic;
    [SerializeField] private LayerMask frameLayer;

    [Header("Debug")]
    [SerializeField] private Status currentStatus;
    [SerializeField] private int currentHp;
    [SerializeField] private int currentStamina;
    [SerializeField] private bool immumeKnockback = false;
    [SerializeField] private bool statusChanged = false;
    [SerializeField] private bool criticalHitted = false;
    [SerializeField] private bool invulnerable = false;

    private Collider2D collider;
    private Rigidbody2D rigidbody;
    private bool isAlive;
    private float originalHpBarScale;
    GameManager gameMng;
    private bool superland;
    private float afterImgInterval = 0.2f, afterImgCnt;
    private int level;
    private bool isknockbacking;
    private bool isPaused;
    private GameObject shieldEffect;
    private Controller player;

    private void Awake()
    {
        shieldEffect = Resources.Load("Prefabs/Shield") as GameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<Controller>();
        collider = GetComponent<Collider2D>();
        rigidbody = GetComponent<Rigidbody2D>();
        currentHp = maxHp;
        isAlive = true;
        criticalHitted = false;
        originalHpBarScale = hpbar.transform.localScale.x;
        gameMng = Object.FindObjectOfType<GameManager>();
        hpbar.DOFade(0.0f, 0.0f);
        hpbarFade.DOFade(0.0f, 0.0f);
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
        graphic.DOColor(Color.blue, 0.0f);
        graphic.DOColor(Color.white, 0.5f);
    }

    public bool DealDamage(int value)
    {
        bool rtn = false;
        currentHp = Mathf.Clamp(currentHp - value, 0, maxHp);

        // rescale hp bar
        hpbar.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 0.0f);
        hpbarFade.transform.DOScaleX(((float)currentHp / (float)maxHp) * originalHpBarScale, 1f);

        // show and fade
        hpbar.DOFade(1.0f, 0.0f);
        hpbarFade.DOFade(1.0f, 0.0f);

        hpbar.DOFade(0.0f, 0.85f);
        hpbarFade.DOFade(0.0f, 0.85f);

        if (currentHp == 0)
        {
            isAlive = false;
            animator.Play(enemyName + "Dead");
            GetComponent<Rigidbody2D>().isKinematic = true;

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

        graphic.DOColor(Color.red, 0.0f);
        graphic.DOColor(Color.white, 0.5f);

        return rtn;
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
        graphic.flipX = direction == 1;

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

    public int GetLevel()
    {
        return level;
    }

    void SuperLanding()
    {
        //gameMng.ScreenImpactGround(0.04f, 0.4f);
        //AudioManager.Instance.PlaySFX("impact", 0.7f);
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

    public bool IsFacingPlayer()
    {
        return ((player.transform.position.x > transform.position.x && !graphic.flipX) || (player.transform.position.x < transform.position.x && graphic.flipX));
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
}
