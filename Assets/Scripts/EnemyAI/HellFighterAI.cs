using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HellFighterAI : EnemyAI
{
    [Header("Setting")]
    [SerializeField] private bool haveTurnAnimation;
    [SerializeField] private int attackDamageBase;
    [SerializeField] private int attackDamageMax;
    [SerializeField] private bool allowAttack;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float staminaRegenInterval;
    [SerializeField] private float dealDamageInterval;

    private EnemyControl controller;

    [Header("References")]
    private Animator animator;
    private LayerMask playerLayer;
    private SpriteRenderer graphic;
    private string enemyName;
    private Controller player;

    [Header("Debug")]
    [SerializeField] private float statusTimer;
    
    bool animationStep;
    float targetMoveX;
    float afterImgInterval = 0.2f;
    float afterImgCnt;
    float staminaRegenCnt;
    float dealDamageCnt;
    private GameObject hellFireEffect;
    private GameObject hellChargeEffect;
    private GameObject hellBurstEffect;
    private GameObject darkFlameEffect;
    private GameObject hellVortexEffect;
    private GameManager gameMng;
    bool firstTimeShockwave = true;
    bool shockwaveHitPlayer = false;

    [SerializeField] int patternCounter = 0;

    private void Awake()
    {
        hellFireEffect = Resources.Load("Prefabs/HellFireBurst") as GameObject;
        hellChargeEffect = Resources.Load("Prefabs/HellCharge") as GameObject;
        hellBurstEffect = Resources.Load("Prefabs/HellBurst") as GameObject;
        darkFlameEffect = Resources.Load("Prefabs/DarkFlame") as GameObject;
        hellVortexEffect = Resources.Load("Prefabs/HellVortex") as GameObject;
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
        patternCounter = 0;
        firstTimeShockwave = true;
        gameMng = FindObjectOfType<GameManager>();

        player = FindObjectOfType<Controller>();

        SetScalingRule(controller.GetLevel());
    }

    private void SetScalingRule(int level)
    {

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
                AttackingCtrl();
                break;
            case Status.Attacked:
                // HELLFIGHTER DON'T GET STUN FROM THIS
                AttackedCtrl();
                break;
            case Status.Turning:
                TurnCtrl();
                break;
            case Status.Chasing:
                ChasingCtrl();
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
                statusTimer = 0.01f; // delay 0.5sec then moveon to next action
                animator.SetBool("Move", false);
                break;
            case Status.Attacking:
                AttackingInitiation();
                break;
            case Status.Attacked:
                statusTimer = controller.FindAnimation(animator, enemyName + "Hit").length / 2f;
                break;
            case Status.Turning:
                break;
            case Status.Chasing:
                controller.SetImmumetoKnockback(true);
                statusTimer = 1f;
                break;
            case Status.Fleeing:
                AudioManager.Instance.PlaySFX(enemyName + "Move");
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
        // TURN IF FACE OPPOSITE SIDE
        if ((player.transform.position.x > controller.transform.position.x
            && graphic.flipX)
            ||
            (player.transform.position.x < controller.transform.position.x
            && !graphic.flipX))
        {
            InitStatus(Status.Turning);
            return;
        }

        // MOVE IN SCREEN IF OUTSIDE OF SCREEN
        if (transform.position.x < - 7f || transform.position.x > 7f)
        {
            InitStatus(Status.Fleeing);
            return;
        }
    
        // DECIDE NEXT MOVE BY PATTERN COUNTER
        InitStatus(Status.Attacking);
        return;
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

    void AttackedCtrl()
    {
        if (statusTimer <= 0.0f)
        {
            InitStatus(Status.Idle);
            return;
        }
    }

    void ChasingCtrl()
    {
        if (statusTimer > 0.0f) return;

        if (!animationStep)
        {
            // MOVE TO MIDDLE
            float direction = transform.position.x > 0f ? -1.0f : 1.0f;
            transform.position = new Vector2(transform.position.x + (direction * moveSpeed * Time.deltaTime * 1.5f), transform.position.y + 0.05f);
            controller.CreateAfterImage();

            AudioManager.Instance.PlaySFX("blink", 0.05f);
            if (Mathf.Abs(transform.position.x) < (direction * moveSpeed * Time.deltaTime * 2f))
            {
                controller.SetImmumetoKnockback(false);
                targetMoveX = (Random.Range(0, 2) * 2 - 1) * 7f;
                statusTimer = 0.1f;
                animationStep = true;
            }
        }
        else
        {
            // MOVE TO EDGE
            controller.SetImmumetoKnockback(true);
            float direction = transform.position.x > targetMoveX ? -1.0f : 1.0f;
            transform.position = new Vector2(transform.position.x + (direction * moveSpeed * Time.deltaTime * 1.5f), transform.position.y + 0.05f);
            controller.CreateAfterImage();
            AudioManager.Instance.PlaySFX("blink", 0.05f);
            if (Mathf.Abs(transform.position.x - targetMoveX) < (direction * moveSpeed * Time.deltaTime * 2f))
            {
                controller.SetImmumetoKnockback(false);
                InitStatus(Status.Idle);
            }
        }
    } 
    
    void AttackingInitiation()
    {
        animationStep = false;

        // DECIDE ATTACK PATTERN WITH PATTERN COUNTER
        if (patternCounter == 0)
        {
            // SHOCKWAVE
            animator.Play(enemyName + "Attack");
            statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length * 2f;
            //Instantiate(hellFireEffect, new Vector2(transform.position.x, transform.position.y + GetComponent<Collider2D>().bounds.size.y /2f), Quaternion.identity);
            Instantiate(hellChargeEffect, new Vector2(transform.position.x, transform.position.y + GetComponent<Collider2D>().bounds.size.y /2f), Quaternion.identity);
            Instantiate(hellVortexEffect, new Vector2(transform.position.x, transform.position.y + GetComponent<Collider2D>().bounds.size.y /2f), Quaternion.identity);

            // AUDIO
            AudioManager.Instance.PlaySFX("hellfightSpell", 0.5f);
        }
        else if (patternCounter == 1)
        {
            // BACKSTAB
            animator.Play(enemyName + "Attack2");
            statusTimer = controller.FindAnimation(animator, enemyName + "Attack2").length - 0.2f;
            Instantiate(hellChargeEffect, new Vector2(transform.position.x, transform.position.y + GetComponent<Collider2D>().bounds.size.y), Quaternion.identity);

            // AUDIO
            AudioManager.Instance.PlaySFX("hellfightSpell", 0.5f);
        }
        else if (patternCounter == 2)
        {
            // ATTACK ONLY
            if ((transform.position.x > player.transform.position.x && !graphic.flipX) ||
                (transform.position.x < player.transform.position.x && graphic.flipX))
            {
                graphic.flipX = !graphic.flipX;
            }
            animator.Play(enemyName + "Attack", 0, 0.2f);
            statusTimer = controller.FindAnimation(animator, enemyName + "Attack").length /2f;
        }
        else if (patternCounter == 3)
        {
            // CHASE
            patternCounter = 0;
            InitStatus(Status.Chasing);
            statusTimer = 0.5f;
            return;
        }
    }

    void AttackingCtrl()
    {
        // STEP 1
        if (!animationStep)
        {
            if (patternCounter == 0 && statusTimer < 1.8f)
            {
                // SHOCKWAVE
                animationStep = true;
                float direction = graphic.flipX ? -1.0f : 1.0f;
                int bounceNum = firstTimeShockwave ? 0: 1;
                firstTimeShockwave = false;
                AudioManager.Instance.PlaySFX("wind", 0.8f);
                shockwaveHitPlayer = false;
                StartCoroutine(Shockwave(Mathf.Max(transform.position.y, -1.7f), transform.position.x + (direction * attackRange), 0.035f, direction * 1f, bounceNum));

                // SHAKE SCREEN
                gameMng.ScreenImpactGround(0.05f, 0.1f);

                // AUDIO
                AudioManager.Instance.PlaySFX("hellslash", 0.4f);
            }
            else if (patternCounter == 1 && statusTimer <= 0.0f)
            {
                // INVISIBLE BACKSTAB
                animationStep = true;
                Instantiate(hellBurstEffect, new Vector2(transform.position.x, transform.position.y + GetComponent<Collider2D>().bounds.size.y / 2f), Quaternion.identity);

                // DEAL DAMAGE
                if (Mathf.Abs(player.transform.position.x - transform.position.x) < 1.5f
                && Mathf.Abs(player.transform.position.y - transform.position.y) < 1.5f)
                {
                    player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1) / 2, transform);
                }

                statusTimer = Random.Range(0.6f, 3f);
                graphic.transform.DOScale(0.0f, 0.0f);

                controller.SetInvulnerable(true);
                controller.SetImmumetoKnockback(true);

                // AUDIO
                AudioManager.Instance.PlaySFX("helljump", 0.4f);
                AudioManager.Instance.PlaySFX("burst", 0.5f);
            }
            else if (patternCounter == 2 && statusTimer <= 0.2f)
            {
                // PLAIN ATTACK
                animationStep = true;
                float direction = graphic.flipX ? -1.0f : 1.0f;
                if (Mathf.Abs(player.transform.position.x - (transform.position.x + (direction * attackRange /2f))) < attackRange * 1.2f
                 && Mathf.Abs(player.transform.position.y - transform.position.y) < attackRange * 1.5f)
                {
                    player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transform);
                    AudioManager.Instance.PlaySFX("hellblade", 0.6f);
                }
                AudioManager.Instance.PlaySFX("wind", 0.8f);
            }
        }
        else // STEP 2
        {
            if (statusTimer == 0.0f)
            {
                if (patternCounter == 1)
                {
                    AudioManager.Instance.PlaySFX("helljump", 0.4f);
                    float direction = graphic.flipX ? -1.0f : 1.0f;
                    transform.position = new Vector2(player.transform.position.x + (direction * attackRange / 3f), transform.position.y + 0.5f);
                    graphic.transform.DOScale(1.0f, 0.5f);
                    controller.SetInvulnerable(false);
                    controller.SetImmumetoKnockback(false);
                    patternCounter = 2;
                    InitStatus(Status.Attacking);
                    return;
                }

                patternCounter++;
                // DECIDE NEXT ACTION
                InitStatus(Status.Idle);
            }
        }
    }

    IEnumerator Shockwave(float y, float initialPosition, float timeinterval, float rangeinterval, int bounceNumber)
    { 
        // WAIT
        yield return new WaitForSeconds(timeinterval);

        // PLAY SPECIAL EFFECT
        Transform transf = Instantiate(hellFireEffect, new Vector2(initialPosition, y), Quaternion.identity).transform;
        transf.GetComponent<SpriteRenderer>().DOFade(0.75f, timeinterval * 10f);
        Destroy(transf.gameObject, timeinterval * 10f);

        Instantiate(darkFlameEffect, new Vector2(initialPosition, y), Quaternion.identity).transform.SetParent(transform.parent);
        
        // PLAY SOUND
        AudioManager.Instance.PlaySFX("burst", 0.10f);

        // DEAL DAMAGE
        if (Mathf.Abs(player.transform.position.x - initialPosition) < 1.0f
            && Mathf.Abs(player.transform.position.y - y) < 1.2f && !shockwaveHitPlayer)
        {
            shockwaveHitPlayer = true;
            player.DealDamage(attackDamageBase + Random.Range(0, attackDamageMax + 1), transf);
        }

        // LOOP
        if (initialPosition < -9f || initialPosition > 9f)
        {
            if (Mathf.Abs(rangeinterval) == 1f && bounceNumber > 0 && !shockwaveHitPlayer)
            {
                StartCoroutine(Shockwave(y, initialPosition - rangeinterval, timeinterval * 2f, -(rangeinterval * 0.5f), bounceNumber-1));
            }
        }
        else
        {
            StartCoroutine(Shockwave(y, initialPosition + rangeinterval, timeinterval, rangeinterval, bounceNumber));
        }
    }

    void FleeingCtrl()
    {
        // TURN IF FACE OPPOSITE SIDE
        if ((player.transform.position.x > controller.transform.position.x
            && graphic.flipX)
            ||
            (player.transform.position.x < controller.transform.position.x
            && !graphic.flipX))
        {
            InitStatus(Status.Turning);
            return;
        }

        // MOVE IN SCREEN IF OUTSIDE OF SCREEN
        float direction = graphic.flipX ? -1.0f : 1.0f;
        if (transform.position.x < -7f || transform.position.x > 7f)
        {
            transform.position = new Vector2(transform.position.x + (direction * moveSpeed * Time.fixedDeltaTime), transform.position.y);
            return;
        }

        // DECIDE NEXT ACTION
        InitStatus(Status.Idle);
    }
}
