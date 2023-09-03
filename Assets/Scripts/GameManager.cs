using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LootLocker.Requests;
using UnityEngine.Events;
using OPS.AntiCheat.Prefs;
using OPS.AntiCheat.Detector;
using Assets.SimpleLocalization;

public class GameManager : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Debug tools")]
    [SerializeField] bool debugMode = false;
    [SerializeField] int debug_initialLevel = 1;
    [SerializeField] bool debug_strongerPlayer = false;
    [SerializeField] bool debug_fixedColor_enable = false;
    [SerializeField] Color debug_fixedColor = new Color(1, 1, 1, 1);
#endif

    [Header("Leaderboard Keys")]
    [SerializeField] string levelLeaderBoardID = "LevelRank";
    [SerializeField] string SpeedRun10LeaderBoardID = "Level10";
    [SerializeField] string SpeedRun20LeaderBoardID = "Level20";
    [SerializeField] string cheaterLeaderboardID = "CheaterRank";

    [Header("References Object")]
    [SerializeField] float musicVolume = 0.7f;
    [SerializeField] int timeCounter;
    [SerializeField] GameObject floatingTextPrefab;
    [SerializeField] Image closeButton;
    [SerializeField] Image timerButton;
    [SerializeField] GameObject openButton;
    [SerializeField] Image restartButton;
    [SerializeField] Image StatusButton;
    [SerializeField] Image leaderBoardButton;
    [SerializeField] Canvas canvas;
    [SerializeField] Controller player;
    [SerializeField] TMP_Text NarrativeText;
    [SerializeField] TMP_Text musicText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text tipsText;
    [SerializeField] TMP_Text timerText;
    [SerializeField] BossHPUI bossHPBar;
    [SerializeField] GameObject hpBar;
    [SerializeField] GameObject staminaBar;
    [SerializeField] GameObject hpBarText;
    [SerializeField] DashChargeBar dashChargeBar;
    [SerializeField] GameObject staminaBarText;
    [SerializeField] SpriteRenderer backgroundFrame;
    [SerializeField] SpriteRenderer background;
    [SerializeField] SpriteRenderer backgroundSprite;
    [SerializeField] EnemySpawner enemySpawner;
    [SerializeField] Collider2D groundCollider;
    [SerializeField] Image screenAlpha;
    [SerializeField] GameObject abilityLearnPanel;
    [SerializeField] GameObject statusMenu;
    [SerializeField] GameObject menuCharacter;
    [SerializeField] Transform frontCanvas;
    [SerializeField] RectTransform GameOverPanel;
    [SerializeField] Image gameOverAlpha;
    [SerializeField] TMP_Text comboText;
    [SerializeField] bool survivorSelected = false;
    [SerializeField] bool potionSelected = false;
    [SerializeField] GameObject menuCanvas;
    [SerializeField] GameObject backCanvas;
    [SerializeField] string playerName;
    [SerializeField] TMP_Text countdownText;
    [SerializeField] Image countdownHourGlassUI;
    [SerializeField] TMP_Text gameoverText;
    [SerializeField] SpriteRenderer monsterAlertArrowLeft;
    [SerializeField] SpriteRenderer monsterAlertArrowRight;
    [SerializeField] Transform playerCorpse;
    [SerializeField] RectTransform itemUI;
    [SerializeField] Image itemUICooldown;
    [SerializeField] ResultScreenUI resultScreenUI;
    [SerializeField] GameObject newUnlockMenu;
    [SerializeField] TMP_Text newUnlockName;
    [SerializeField] TMP_Text newUnlockDescription;
    [SerializeField] Image newUnlockIcon;
    [SerializeField] Image newUnlockAlpha;
    [SerializeField] NewGroundAPI newGroundsAPI;
    [SerializeField] string[] musicList;
    [SerializeField] string[] bossMusicList;
    [SerializeField] Color[] bossColorThemeList;
    [SerializeField] Transform[] parallaxList;

    [Header("Game Configuaration")]
    [SerializeField] int initialTime = 50;
    [SerializeField] int extraTimePerLevel = 15;

    [Header("Debug")]
    [SerializeField] int currentLevel;
    [SerializeField] int totalNumberOfMonster;
    [SerializeField] List<EnemyControl> monsterList;
    [SerializeField] List<GameObject> extraStuffList; // object to get rid of when player restart 
    [SerializeField] bool spawnEnded = false;
    [SerializeField] bool levelEnded = false;
    [SerializeField] bool gameOver = false;
    [SerializeField] string lastMusic = string.Empty;
    [SerializeField] Skill[] lastSpawnedSkill = new Skill[3];

    private GameObject fireburstEffect;
    private bool allowRestart;
    private int monsterSpawnCount;
    Coroutine roundTimer;
    Coroutine timeCounterCoroutine;
    List<UnlockData> newUnlock = new List<UnlockData>();
    public PlayerAction _input;
    bool confrimKey = false;
    bool isRoundTimerRunning = false;
    bool isAddTimerEffectRunning = false;

    int timer;

    // Start is called before the first frame update
    void Awake()
    {
        DOTween.SetTweensCapacity(1250, 50);
        monsterList = new List<EnemyControl>();
        fireburstEffect = Resources.Load("Prefabs/FireBurst") as GameObject;

        // CHEAT DETECTION
        FieldCheatDetector.OnFieldCheatDetected += FieldCheatDetector_OnFieldCheatDetected;

        // INPUT SYSTEM
        _input = new PlayerAction();
        _input.MenuControls.AnyKey.performed += ctx => confrimKey = true;
        _input.MenuControls.AnyKey.canceled += ctx => confrimKey = false;
    }
    private void OnDisable()
    {
        _input.Disable();
    }
    private void OnEnable()
    {
        _input.Enable();
    }

    // Cheat detection
    private void FieldCheatDetector_OnFieldCheatDetected()
    {
        // field modification detected
        levelLeaderBoardID = cheaterLeaderboardID;
        SpeedRun10LeaderBoardID = cheaterLeaderboardID;
        SpeedRun20LeaderBoardID = cheaterLeaderboardID;
    }

    public void Initialize()
    {
        allowRestart = false;
        backgroundFrame.color = new Color(0, 0, 0, 0);
        openButton.SetActive(false);
        lastSpawnedSkill = new Skill[3];
        newUnlock.Clear();
        for (int i = 0; i < 3; i++)
        {
            lastSpawnedSkill[i] = Skill.ComboMaster;
        }
        resultScreenUI.Initialization(this);
    }

    public void SpawnFloatingText(Vector2 loc, float time, float _speed, string text, Color color, Vector2 direction, float fontSize)
    {
        var tmp = Instantiate(floatingTextPrefab);
        tmp.transform.localPosition = loc;
        tmp.GetComponent<floaitngtext>().Initialize(time, _speed, text, color, direction, fontSize);
    }

    public void ShakeHPBar(int count)
    {
        hpBarText.GetComponent<HpText>().StartShake(count);
    }

    public void ShakeStaminaBar(int count)
    {
        staminaBarText.GetComponent<StaminaText>().StartShake(count);
    }

    public void UseDashCharge()
    {
        player.SetCurrentDashCharge(Mathf.Max(player.GetCurrentDashCharge() - 1, 0));
        dashChargeBar.UseDashCharge();
    }

    public void RecoverDashCharge(int num, bool instant)
    {
        player.SetCurrentDashCharge(player.GetCurrentDashCharge() + num);
        dashChargeBar.RecoverDashCharge(num, instant);
    }
    public void RecoverAllDashCharge(bool instant)
    {
        player.SetCurrentDashCharge(player.GetMaxDashCharge());
        dashChargeBar.RecoverAllDashSlot(instant);
    }

    public static Vector3 WorldToScreenSpace(Vector3 worldPos, Camera cam, RectTransform area)
    {
        Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
        screenPoint.z = 0;

        Vector2 screenPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, cam, out screenPos))
        {
            return screenPos;
        }

        return screenPoint;
    }

    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        // Reset flags
        survivorSelected = false;
        potionSelected = false;
        screenAlpha.DOFade(1.0f, 0.0f);
        screenAlpha.DOFade(0.0f, 1.0f);
        menuCanvas.SetActive(false);
        backCanvas.SetActive(true);
        frontCanvas.gameObject.SetActive(true);
        SetStatusBarVisible(false);
        player.gameObject.SetActive(false);
        newUnlock.Clear();
        currentLevel = 1;
        timer = initialTime;
        bossHPBar.Initialize();

        // Animation
        Coroutine cinematic = StartCoroutine(StartGameCinematic());

        // Progress manager
        ProgressManager.Instance().UpdateHighestLevel(currentLevel);

        // Result manager
        ResultManager.Instance().GameReset();

        // Steam API call
        SteamworksNetManager.Instance().SetSteamRichPresence(true, currentLevel); // level 1

#if UNITY_EDITOR
        // Debug Tools
        if (debugMode)
        {
            if (debug_initialLevel > 1)
            {
                player.gameObject.SetActive(true);
                currentLevel = debug_initialLevel;
                StopCoroutine(cinematic);

                // UI
                {
                    levelText.DOFade(1.0f, 1.0f);
                    NarrativeText.color = new Color(1.0f, 0.9f, 0.0f, 0.0f);
                    NarrativeText.DOFade(1.0f, 2.0f);
                    StartLevelCinematic();
                    SetStatusBarVisible(true);
                    StartLevel(currentLevel);
                    SteamworksNetManager.Instance().UpdateLevelStat(currentLevel);

                    levelText.SetText("Level " + currentLevel.ToString());
                    timerText.gameObject.SetActive(false);
                }

                // BGM
                {
                    AudioManager.Instance.SetMusicVolume(musicVolume);
                    lastMusic = RandomMusic();
                    if (currentLevel % 10 == 0)
                    {
                        // boss music
                        lastMusic = bossMusicList[(currentLevel / 10) - 1];
                    }
                    musicText.SetText("♪ - " + lastMusic);
                    musicText.DOFade(1.0f, 1.0f);
                    AudioManager.Instance.PlayMusic(lastMusic);
                }
            }

            if (debug_strongerPlayer)
            {
                player.ApplyBonus(Skill.BaseDamage, currentLevel * 0.6f);
                player.ApplyBonus(Skill.LightningLash, 10.0f);
                player.ApplyBonus(Skill.Potion, 10.0f);
                player.ApplyBonus(Skill.MoveSpeed, 2.0f);
                player.ApplyBonus(Skill.Windrunner, Mathf.Floor(currentLevel / 10));
                player.ApplyBonus(Skill.Survivor, 1.0f);
                player.ApplyBonus(Skill.Stamina, currentLevel * 2.5f);
                player.ApplyBonus(Skill.Vitality, currentLevel * 2f);
                player.ApplyBonus(Skill.Berserker, 5.0f);
                player.ApplyBonus(Skill.Deflect, 20.0f);
                player.ApplyBonus(Skill.DashCooldown, 50.0f);
                player.ApplyBonus(Skill.DashDamage, (float)currentLevel);
            }
        }
#endif
    }

    IEnumerator StartGameCinematic()
    {
        yield return new WaitForSeconds(0.5f);
        NarrativeText.color = new Color(1.0f, 0.9f, 0.0f, 0.0f);
        NarrativeText.DOFade(1.0f, 2.0f);

        string narrative = LocalizationManager.Localize("Text.NewChallenger");
        if (ProgressManager.Instance().GetHighestLevelRecord() > 1)
        {
            narrative = LocalizationManager.Localize("Text.Retry");
        }
        NarrativeText.SetText(narrative);

        backgroundFrame.DOColor(new Color(1, 1, 1, 1), 2.0f);
        AudioManager.Instance.PlaySFX("doon!");

        // check what player use as a input method by detecting his next input and get a call back
        ControlPattern.Instance().DetectControlPattern();

        yield return new WaitForSeconds(2.0f);
        StartLevelCinematic();

        yield return new WaitForSeconds(0.2f);
        SetStatusBarVisible(true);

        if (FBPP.GetBool("IsTutorialFinished", false))
        {
            StartLevel(currentLevel);
        }
        else
        {
            StartCoroutine(StartTutorial());
        }
        SteamworksNetManager.Instance().UpdateLevelStat(currentLevel);

        yield return new WaitForSeconds(2.0f);
        levelText.DOFade(1.0f, 1.0f);
    }

    public void SetStatusBarVisible(bool boolean)
    {
        hpBar.SetActive(boolean);
        staminaBar.SetActive(boolean);
        hpBarText.SetActive(boolean);
        staminaBarText.SetActive(boolean);
        dashChargeBar.gameObject.SetActive(boolean);

        // only show item UI if potion is available
        if (IsPotionSelected() || !boolean)
        {
            itemUI.gameObject.SetActive(boolean);
        }
    }

    /// <summary>
    /// タイマーの時間を増加
    /// </summary>
    /// <param name="time"></param>
    public void TimerAddTime(int time)
    {
        // Extra time
        if (isRoundTimerRunning)
        {
            StartCoroutine(AddTimerEffect(time));
        }
        else
        {
            timer += time;
        }
    }

    /// <summary>
    /// レベル変更時用、タイマーの時間を増加
    /// </summary>
    public void TimerAddTimeNewLevel()
    {
        TimerAddTime(extraTimePerLevel);
    }

    /// <summary>
    /// インパクトを表現するためのスクリーンシェイク
    /// </summary>
    /// <param name="time"></param>
    /// <param name="magnitude"></param>
    public void ScreenImpactGround(float time, float magnitude)
    {
        Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - magnitude, Camera.main.transform.position.z);
        StartCoroutine(ReturnScreenPosition(time));
    }

    /// <summary>
    /// レベル変更時に背景のテーマと色を設定
    /// </summary>
    public void ScreenChangeTheme()
    {
        if (currentLevel == 1) return;

        // Get Preset color
        Transform selectedParallax = RandomParallax();
        Color presetColor = selectedParallax.GetComponent<BackgroundColorPalette>().colorSchemeList[Random.Range(0, selectedParallax.GetComponent<BackgroundColorPalette>().colorSchemeList.Count)];

#if UNITY_EDITOR
        if (debugMode && debug_fixedColor_enable) presetColor = debug_fixedColor;
#endif

        // ボス戦は特定の背景色を設定
        // Boss preset color
        if (currentLevel % 10 == 0)
        {
            presetColor = bossColorThemeList[(currentLevel / 10) - 1];
        }

        backgroundFrame.color = presetColor;
        background.color = new Color(presetColor.r / 10f, presetColor.g / 10f, presetColor.b / 10f, 1.0f);
        backgroundSprite.color = new Color(presetColor.r / 10f, presetColor.g / 10f, presetColor.b / 10f, 1.0f);
        bossHPBar.ChangeFrameColor(new Color(presetColor.r * 0.5f, presetColor.g * 0.5f, presetColor.b * 0.5f, 1.0f));

        musicText.color = presetColor;
        levelText.color = presetColor;
        timerText.color = presetColor;
        countdownText.color = new Color(presetColor.r, presetColor.g, presetColor.b, 0.5f); ;

        // 特定のレベル以外は背景がランダム
        // level specific theme
        if (currentLevel == 5)
        {
            backgroundSprite.GetComponent<Animator>().enabled = true;
            backgroundSprite.color = new Color(GetThemeColor().r * 1.5f, GetThemeColor().g * 1.5f, GetThemeColor().b * 1.5f, 1.0f);
            ResetParallax();
        }
        else if (currentLevel == 6)
        {
            backgroundSprite.GetComponent<Animator>().enabled = false;
            SetBackground(string.Empty);
            SetParallax(selectedParallax, presetColor);
        }
        else if (currentLevel == 10)
        {
            SetBackground("lake");
            ResetParallax();
        }
        else if (currentLevel == 15)
        {
            SetBackground("blue");
            ResetParallax();
        }
        else if (currentLevel == 20)
        {
            SetBackground("cemetery");
            ResetParallax();
        }
        else if (currentLevel == 25)
        {
            SetBackground("purple");
            ResetParallax();
        }
        else if (currentLevel == 30)
        {
            SetBackground("thehungryone");
            ResetParallax();
        }
        else if (currentLevel == 40)
        {
            SetBackground("eclipse");
            ResetParallax();
        }
        else
        {
            SetBackground(string.Empty);
            SetParallax(selectedParallax, presetColor);
        }
    }

    /// <summary>
    /// スクリーンポジションを初期化
    /// </summary>
    IEnumerator ReturnScreenPosition(float delay)
    {
        yield return new WaitForSeconds(delay);

        Camera.main.transform.position = new Vector3(0, 0, -10);
    }

    /// <summary>
    /// シナリオ開始
    /// </summary>
    public void StartLevelCinematic()
    {
        screenAlpha.DOFade(0.0f, 1.0f);
        NarrativeText.SetText(string.Empty);
        player.gameObject.SetActive(true);
        player.transform.position = new Vector2(Mathf.Clamp(player.transform.position.x, -8f, 8f), 6.9f);
        player.GetComponent<Rigidbody2D>().velocity = new Vector2(0.0f, -60f);
    }

    public void NextLevel()
    {
        currentLevel++;

        // PLAYER DIE HERE LAST TIME?
        playerCorpse.gameObject.SetActive(false);
        if (FBPP.GetInt("LastDeath", -1) == currentLevel)
        {
            // player die on here last time
            playerCorpse.position = new Vector2(FBPP.GetFloat("LastDeathPosition", 0.0f), playerCorpse.position.y);
            playerCorpse.GetComponent<SpriteRenderer>().flipX = intToBool(FBPP.GetInt("LastDeathFlip", 0));
            playerCorpse.gameObject.SetActive(true);

            FBPP.DeleteKey("LastDeath");
            FBPP.DeleteKey("LastDeathPosition");
            FBPP.DeleteKey("LastDeathFlip");
        }

        // LEVEL TEXT
        levelText.SetText("Level " + currentLevel.ToString());
        musicText.DOFade(1.0f, 1.0f);
        if (FBPP.GetInt("ShowTimer", 0) == 1) timerText.DOFade(1.0f, 1.0f);
        StartLevel(currentLevel);

        // CHANGE BGM
        AudioManager.Instance.SetMusicVolume(musicVolume);
        lastMusic = RandomMusic();
        if (currentLevel % 10 == 0)
        {
            // boss music
            lastMusic = bossMusicList[(currentLevel / 10) - 1];
        }
        musicText.SetText("♪ - " + lastMusic);
        musicText.DOFade(1.0f, 1.0f);
        AudioManager.Instance.PlayMusic(lastMusic);

        //NEW GROUNDAPI
        newGroundsAPI.NGUnlockMedal(65056);
        if (currentLevel == 11)
        {
            newGroundsAPI.NGUnlockMedal(65057);
        }
        if (currentLevel == 21)
        {
            newGroundsAPI.NGUnlockMedal(65058);
        }
        if (currentLevel == 35)
        {
            newGroundsAPI.NGUnlockMedal(65098);
        }
        if (player.GetMaxHP() >= 100)
        {
            newGroundsAPI.NGUnlockMedal(65096);
        }
        if (player.GetMaxStamina() >= 200)
        {
            newGroundsAPI.NGUnlockMedal(65095);
        }

        // Progress manager
        ProgressManager.Instance().UpdateHighestLevel(currentLevel);

        // Result Manager
        ResultManager.Instance().SetLevelReached(currentLevel);

        // STEAMWORKS API
        SteamworksNetManager.Instance().UpdateLevelStat(currentLevel);
       // SteamworksNetManager.Instance().CheckLevelAchievement(currentLevel);
        SteamworksNetManager.Instance().SetSteamRichPresence(true,currentLevel);

        // SAVE GAME
        FBPP.Save();
    }

    public Transform RandomParallax()
    {
        List<int> list = new List<int>();

        for (int i = 0; i < parallaxList.Length; i++)
        {
            if (!parallaxList[i].gameObject.activeSelf)
            {
                list.Add(i);
            }
        }
        return parallaxList[list[Random.Range(0, list.Count)]];
    }

    public string RandomMusic()
    {
        List<int> list = new List<int>();

        for (int i = 0; i < musicList.Length; i++) if (musicList[i] != lastMusic) list.Add(i);

        return musicList[list[Random.Range(0, list.Count)]];
    }

    private void StartLevel(int level)
    {
        openButton.SetActive(true);
        openButton.GetComponent<Image>().DOFade(0.0f, 0.0f);
        openButton.GetComponent<Image>().DOFade(1.0f, 0.5f);
        spawnEnded = false;
        levelEnded = false;
        monsterList.Clear();
        extraStuffList.Clear();
        enemySpawner.ResetSpawner();
        totalNumberOfMonster = enemySpawner.StartSpawning(level);
        groundCollider.enabled = true;
        monsterSpawnCount = 0;

        if (level % 10 != 0 && level > 1)
        {
            roundTimer = StartCoroutine(LevelTimer(level));
            isRoundTimerRunning = true;
        }
        timeCounterCoroutine = StartCoroutine(TimeCounterLoop());

        // Reset Survivor
        if (level % 10 == 1)
        {
            survivorSelected = false;
        }

        // Tips and narrative
        int record = ProgressManager.Instance().GetHighestLevelRecord();
        if (level == 1 && (record <= level))
        {
            
        }
        else if (level == 2 && record <= level)
        {
            if (ControlPattern.Instance().GetControlPattern() == ControlPattern.CtrlPattern.JOYSTICK)
            {
                tipsText.SetText("<font=pixelinput SDF>0</font> " + LocalizationManager.Localize("Tutorial.Status"));
            }
            else
            {
                tipsText.SetText("<font=pixelinput SDF>q</font> " + LocalizationManager.Localize("Tutorial.Status"));
            }
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 5 && record <= level)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.TipsLevel5"));
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 8 && record <= level)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.TipsLevel8"));
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 10 && record <= level)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.TipsLevel10"));
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 15 && record <= level)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.TipsLevel15"));
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 25 && record <= level)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.TipsLevel25"));
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (potionSelected && !FBPP.GetBool("TutorialPotion", false))
        {
            FBPP.SetBool("TutorialPotion", true);
            if (ControlPattern.Instance().IsJoystickConnected())
            {
                tipsText.SetText("\n<font=pixelinput SDF>+</font> " + LocalizationManager.Localize("Tutorial.UseItem"));
            }
            else
            {
                tipsText.SetText("<font=pixelinput SDF>c</font> " + LocalizationManager.Localize("Tutorial.UseItem"));
            }
            FBPP.Save();
        }
        else
        {
            tipsText.SetText(string.Empty);
            tipsText.DOFade(0.0f, 0.5f);
        }
    }

    IEnumerator StartTutorial()
    {
        // teach player to move
        bool nextStep = false;
        if (ControlPattern.Instance().IsJoystickConnected() && ControlPattern.Instance().GetControlPattern() == ControlPattern.CtrlPattern.JOYSTICK)
        {
           // tipsText.SetText(Input.GetJoystickNames()[0] + "\n<font=pixelinput SDF>4</font> " + LocalizationManager.Localize("Tutorial.Attack") + " <font=pixelinput SDF>6</font> " + LocalizationManager.Localize("Tutorial.Jump") + " <font=pixelinput SDF>7</font> " + LocalizationManager.Localize("Tutorial.Dash"));
            tipsText.SetText(LocalizationManager.Localize("Tutorial.Jump") + "\n<font=pixelinput SDF>6</font>");
        }
        else
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.Move") + "\n<font=pixelinput SDF><size=1> </size>W  <size=11> </size>w \nASD asd </font>");
        }
        tipsText.DOFade(1.0f, 0.5f);

        while (!nextStep)
        {
            if (player.IsJumping() && ControlPattern.Instance().IsJoystickConnected()) nextStep = true;
            if (player.GetCurrentInputMove() != 0.0f && !ControlPattern.Instance().IsJoystickConnected()) nextStep = true;
            yield return new WaitForEndOfFrame();
        }

        nextStep = false;
        tipsText.DOFade(0.0f, 0.5f);
        yield return new WaitForSeconds(1.0f);

        // teach to dash
        if (ControlPattern.Instance().IsJoystickConnected() && ControlPattern.Instance().GetControlPattern() == ControlPattern.CtrlPattern.JOYSTICK)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.Dash") + "\n<font=pixelinput SDF>7</font>");
        }
        else
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.Dash") + "\n<font=pixelinput SDF>x</font>");
        }
        tipsText.DOFade(1.0f, 0.5f);

        while (!nextStep)
        {
            if (player.IsDashing()) nextStep = true;
            yield return new WaitForEndOfFrame();
        }
        tipsText.DOFade(0.0f, 0.5f);

        nextStep = false;
        yield return new WaitForSeconds(1.0f);
        
        // teach to attack
        StartLevel(currentLevel);
        if (ControlPattern.Instance().IsJoystickConnected() && ControlPattern.Instance().GetControlPattern() == ControlPattern.CtrlPattern.JOYSTICK)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.Attack") + "\n<font=pixelinput SDF>4</font>");
        }
        else
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.Attack") + "\n<font=pixelinput SDF>z</font>");
        }
        tipsText.DOFade(1.0f, 0.5f);

        while (!nextStep)
        {
            if (levelEnded) nextStep = true;
            yield return new WaitForEndOfFrame();
        }
        
        FBPP.SetBool("IsTutorialFinished", true);
        FBPP.Save();
    }

    private void EndLevel()
    {
        monsterSpawnCount = 0;
        monsterList.Clear();
        extraStuffList.Clear();
        spawnEnded = false;
        levelEnded = true;
        if (isRoundTimerRunning)
        {
            isRoundTimerRunning = false;
            StopCoroutine(roundTimer);
        }
        StopCoroutine(timeCounterCoroutine);

        if (player.IsJumping())
        {
            PlayerJumped();
        }

        // NARRATIVE TEXT
        NarrativeText.color = new Color(0.75f, 0.0f, 0.0f, 0.0f);
        NarrativeText.DOFade(1.0f, 0.5f);
        NarrativeText.SetText(LocalizationManager.Localize("Text.Proceed"));
        if (currentLevel == 9) NarrativeText.SetText(LocalizationManager.Localize("Text.Proceedlvl9"));
        if (currentLevel == 19) NarrativeText.SetText(LocalizationManager.Localize("Text.Proceedlvl19"));
        if (currentLevel == 29) NarrativeText.SetText(LocalizationManager.Localize("Text.Proceedlvl29"));
        // TIPS TEXT
        if (currentLevel == 1)
        {
            tipsText.DOFade(0.0f, 0.4f);
        }

        // SE
        AudioManager.Instance.PlaySFX("proceed");
        AudioManager.Instance.StopMusicWithFade(0.0f);
    }

    public void PlayerJumped()
    {
        if (levelEnded)
        {
            groundCollider.enabled = false;
        }
    }

    public void RegisterMonsterInList(EnemyControl monster)
    {
        monsterList.Add(monster);
        monsterSpawnCount++;
        if (monsterSpawnCount >= totalNumberOfMonster)
        {
            spawnEnded = true;
        }
    }

    public void MonsterDied(EnemyControl monster)
    {
        monsterList.Remove(monster);
        Destroy(monster.gameObject);

        if (monsterList.Count == 0 && spawnEnded)
        {
            EndLevel();
        }
    }

    public void RregisterBoss(EnemyControl monster)
    {
        bossHPBar.Activate(monster);
    }

    public void RegisterExtraStuff(GameObject reference)
    {
        extraStuffList.Add(reference);
    }

    // player reach the botton -> let player pick new powerup -> next level
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Controller>() != null)
        {
            player.Regenerate(player.GetHPRegen()/* + (player.GetMaxHP() / 10)*/, player.GetStaminaMax(), false);
            player.RecoverAllDashCharge(false);
            player.SetInvulnerable(false);
            itemUICooldown.fillAmount = 1f;
            openButton.SetActive(false);
            screenAlpha.DOFade(0.9f, 1.0f);
            collision.gameObject.SetActive(false);
            GameObject tmp = Instantiate(abilityLearnPanel, frontCanvas.transform);
            tmp.GetComponent<AbilityLearnPanel>().Initialize(currentLevel, this, player);

            // SPEED RUNNER
            if (currentLevel == 10)
            {
                float besttime = FBPP.GetFloat("SpeedRunLevel10", 7200f);

                if (besttime > timeCounter)
                {
                    FBPP.SetFloat("SpeedRunLevel10", timeCounter);
                }
            }
            if (currentLevel == 20)
            {
                float besttime = FBPP.GetFloat("SpeedRunLevel20", 7200f);

                if (besttime > timeCounter)
                {
                    FBPP.SetFloat("SpeedRunLevel20", timeCounter);
                }
            }
            FBPP.Save();
        }
    }

    public List<EnemyControl> GetMonsterList()
    {
        return monsterList;
    }

    public void OpenMenu()
    {
        AudioManager.Instance.PlaySFX("menuOpen");
        statusMenu.GetComponent<StatusMenuUpdate>().UpdateValue((int)timeCounter);
        screenAlpha.DOFade(0.8f, 0.25f).SetUpdate(true);
        statusMenu.GetComponent<RectTransform>().DOScaleX(1.0f, 0.25f).SetUpdate(true);
        float originalPos = closeButton.GetComponent<RectTransform>().anchoredPosition.x;
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, closeButton.GetComponent<RectTransform>().anchoredPosition.y);
        closeButton.GetComponent<RectTransform>().DOAnchorPosX(originalPos, 0.25f, false).SetUpdate(true);
        closeButton.DOFade(1.0f, 0.5f).SetUpdate(true);

        originalPos = timerButton.GetComponent<RectTransform>().anchoredPosition.x;
        timerButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, closeButton.GetComponent<RectTransform>().anchoredPosition.y);
        timerButton.GetComponent<RectTransform>().DOAnchorPosX(originalPos, 0.25f, false).SetUpdate(true);
        timerButton.DOFade(1.0f, 0.5f).SetUpdate(true);
        timerButton.GetComponent<Button>().enabled = true;

        // pause game
        player.Pause(true);
        foreach (EnemyControl enemy in monsterList)
        {
            enemy.Pause(true);
        }

        AudioManager.Instance.PauseMusic();
        Time.timeScale = 0.0f;
    }

    public void CloseMenu()
    {
        AudioManager.Instance.PlaySFX("menuClose");

        screenAlpha.DOFade(0.0f, 0.25f).SetUpdate(true);
        statusMenu.GetComponent<RectTransform>().DOScaleX(0.0f, 0.25f).SetUpdate(true);
        closeButton.DOFade(0.0f, 0.0f).SetUpdate(true);
        timerButton.DOFade(0.0f, 0.0f).SetUpdate(true);

        // unpause game
        if (!gameOver)
        {
            Time.timeScale = 1.0f;
            player.Pause(false);
            foreach (EnemyControl enemy in monsterList)
            {
                enemy.Pause(false);
            }
        }

        if (!levelEnded)
        {
            AudioManager.Instance.UnpauseMusic();
        }
    }

    public void ShowHideTimer()
    {
        timerText.gameObject.SetActive(!timerText.gameObject.activeSelf);

        if (timerText.gameObject.activeSelf)
        {
            timerText.DOFade(1.0f, 0.0f).SetUpdate(true);
        }

        FBPP.SetInt("ShowTimer", timerText.gameObject.activeSelf ? 1 : 0);
        FBPP.Save();
    }

    public void GameOver()
    {
        // STOP COUNTING TIME
        if (isRoundTimerRunning)
        {
            isRoundTimerRunning = false;
            StopCoroutine(roundTimer);
        }
        StopCoroutine(timeCounterCoroutine);

        // STOP BGM
        AudioManager.Instance.StopMusicWithFade(1.0f);

        // DISABLE TOP-RIGHT MENU BUTTON
        openButton.SetActive(false);

        // Game progress
        gameOverAlpha.DOFade(0.8f, 0.5f).SetUpdate(true);
        resultScreenUI.gameObject.SetActive(true);
        resultScreenUI.ResetUI();
        resultScreenUI.StartAnimation(ResultManager.Instance().GetResult());
        StartCoroutine(WaitForResultScreenFinish());

        // Reset Text UI
        NarrativeText.text = string.Empty;
        comboText.text = string.Empty;

        // PAUSE GAME
        Time.timeScale = 0.0f;

        // FLAG
        gameOver = true;
        allowRestart = true;

        // SAVE LOCAL DATA
        FBPP.SetInt("LastDeath", currentLevel);
        FBPP.SetFloat("LastDeathPosition", player.transform.position.x);
        FBPP.SetInt("LastDeathFlip", boolToInt(player.IsFlip()));
        FBPP.Save();

#if UNITY_WEBGL
        // SUBMIT NEWGROUNDS SCOREBOARD
        newGroundsAPI.NGSubmitScore(10762, currentLevel);
#else
        // SUBMIT LOOTLOCKER SCOREBOARD
        gameoverText.SetText(string.Empty);
        int rank = UploadToLeaderBoard(LeaderboardType.Level);
#endif
    }

    IEnumerator WaitForResultScreenFinish()
    {
        while (!resultScreenUI.IsFinished())
        {
            yield return null;
        }
        
        // determine need to enter new unlock phase or not
        newUnlock = ProgressManager.Instance().GetNewUnlockList();
        if (newUnlock.Count > 0)
        {
            // Go to unlock screen
            StartCoroutine(NewUnlockMenuPhase(RestartDelay(0.5f)));
        }
        else
        {
            // Go to game over panel
            GameOverPanel.gameObject.SetActive(true);
            menuCharacter.GetComponent<Animator>().Play("playerUIDead");
            menuCharacter.GetComponent<RectTransform>().DOAnchorPosX(27f, 0.0f, false).SetUpdate(true);
        }
    }

    private int UploadToLeaderBoard(LeaderboardType type)
    {
        int rank = -1;

        //string memberID = "[name=" + playerName + "]";
        //memberID += "[version=" + Application.version + "]";
        //memberID += "[date=" + System.DateTime.Now.ToString("MM/dd") + "]";
        //memberID += "[hp=" + player.GetMaxHP().ToString() + "]";
        //memberID += "[hpregen=" + player.GetHPRegen().ToString() + "]";
        //memberID += "[stamina=" + player.GetMaxStamina().ToString() + "]";
        //memberID += "[movespeed=" + player.GetMoveSpeed().ToString() + "]";
        //memberID += "[atkDmg=" + player.GetAttackDamage().ToString() + " ~ " + (player.GetAttackDamage()+player.GetMaxDamage()).ToString() + "]";
        //memberID += "[dashDmg=" + player.GetDashDamage().ToString() + "]";
        //memberID += "[dashCD=" + player.GetDashCD().ToString() + "]";
        //memberID += "[critical=" + player.GetCritical().ToString() + "]";
        //memberID += "[lifesteal=" + player.GetLightningLash().ToString() + "]";
        //memberID += "[lifedrain=" + player.GetLifeDrain().ToString() + "]";
        //memberID += "[combomaster=" + player.GetComboMaster().ToString() + "]";
        //memberID += "[localtime=" + System.DateTime.Now.ToLocalTime().ToString() + "]";

        // name only
        string memberID = playerName;
        string leaderboardID;
        int data;
        switch (type)
        {
            case LeaderboardType.Level:
                leaderboardID = GetLeaderboardKey(type);
                data = currentLevel;
                break;
            case LeaderboardType.SpeedRunLevel10:
                leaderboardID = SpeedRun10LeaderBoardID.ToString();
                data = Mathf.RoundToInt(FBPP.GetFloat("SpeedRunLevel10", 7200f));
                break;
            case LeaderboardType.SpeedRunLevel20:
                leaderboardID = SpeedRun20LeaderBoardID.ToString();
                data = Mathf.RoundToInt(FBPP.GetFloat("SpeedRunLevel20", 7200f));
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                data = 0;
                leaderboardID = string.Empty;
                return rank;
        }

        // record leaderboard
        LootLockerSDKManager.SubmitScore(memberID, data, leaderboardID, (response) =>
        {
            Debug.Log("ハイスコア提出中");
            if (response.success)
            {
                Debug.Log("リーダーボード更新成功");
                rank = response.rank;
                data = response.score;

                SetGameOverText(type, rank, data);

                if (currentLevel > 10 && type == LeaderboardType.Level) UploadToLeaderBoard(LeaderboardType.SpeedRunLevel10);
                if (currentLevel > 20 && type == LeaderboardType.SpeedRunLevel10) UploadToLeaderBoard(LeaderboardType.SpeedRunLevel20);
            }
            else
            {
                Debug.Log("リーダーボード　サーバー接続失敗！！");
                gameoverText.SetText("");
                rank = -1;
            }
        });

        return rank;
    }

    private void SetGameOverText(LeaderboardType type, int rank, int data)
    {
        int min, sec;
        string extraString = string.Empty;
        switch (type)
        {
            case LeaderboardType.Level:
                gameoverText.SetText(LocalizationManager.Localize("Result.LevelRank"), rank, data);
                break;
            case LeaderboardType.SpeedRunLevel10:
                min = data / 60;
                sec = data % 60;
                extraString = string.Format(LocalizationManager.Localize("Result.Level10Rank"), rank, min, sec);
                gameoverText.SetText(gameoverText.text + "\n" + extraString);
                break;
            case LeaderboardType.SpeedRunLevel20:
                min = data / 60;
                sec = data % 60;
                extraString = string.Format(LocalizationManager.Localize("Result.Level20Rank"), rank, min, sec);
                gameoverText.SetText(gameoverText.text + "\n" + extraString);
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }

        //　日本語の行替えを別で設定
        if (LocalizationManagerHellFight.Instance().GetCurrentLanguage() == "Japanese")
        {
            gameoverText.lineSpacing = -9.0f;
        }
    }

    public void RestartGame()
    {
        if (!allowRestart) return;
        allowRestart = false;
        screenAlpha.DOFade(1.0f, 0.5f).SetUpdate(true);
        AudioManager.Instance.PlaySFX("restart");

        // Restart Animation
        Time.timeScale = 1.0f;
        StartCoroutine(RestartDelay(2.0f));
    }

    IEnumerator NewUnlockMenuPhase(IEnumerator nextPhase)
    {
        newUnlockAlpha.DOFade(1.0f, 0.0f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.5f);
        // CHECK IF NEW STUFF UNLOCKED
        newUnlockMenu.SetActive(true);

        bool showMenu = true;
        while (showMenu)
        {
            newUnlockAlpha.DOFade(0.0f, 0.25f).SetUpdate(true);
            AudioManager.Instance.PlaySFX("Unlock", 0.85f);
            UnlockData data = newUnlock[0];
            newUnlockIcon.sprite = Resources.Load<Sprite>("Icon/" + data.unlock_Icon);
            newUnlockName.SetText(data.unlock_name);
            newUnlockDescription.SetText(data.unlock_description);

            while (!IsConfirmKeyPressed())
            {
                yield return null;
            }
            ResetConfirmKey(); // reset input

            if (newUnlock.Count == 1)
            {
                showMenu = false;
            }
            else
            {
                newUnlockAlpha.DOFade(1.0f, 0.25f).SetUpdate(true);
                yield return new WaitForSecondsRealtime(0.25f);
                newUnlock.RemoveAt(0);
            }
        }

        newUnlockAlpha.DOFade(1.0f, 0.5f).SetUpdate(true);

        yield return new WaitForSecondsRealtime(1.0f);

        newUnlockMenu.SetActive(false);
        
        // Game Over Panel
        GameOverPanel.gameObject.SetActive(true);
        menuCharacter.GetComponent<Animator>().Play("playerUIDead");
        menuCharacter.GetComponent<RectTransform>().DOAnchorPosX(27f, 0.0f, false).SetUpdate(true);
    }

    IEnumerator RestartDelay(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        // initialize
        // Reset Color
        background.color = new Color(0, 0, 0, 1);
        backgroundFrame.color = new Color(0, 0, 0, 0);
        backgroundSprite.GetComponent<Animator>().enabled = false;
        backgroundSprite.sprite = null;
        ResetParallax();

        // Remove All Monster
        enemySpawner.ResetSpawner();
        foreach (EnemyControl enemy in monsterList)
        {
            Destroy(enemy.gameObject);
        }
        monsterList.Clear();
        foreach (GameObject obj in extraStuffList)
        {
            Destroy(obj);
        }

        // Reset Player
        player.transform.position = new Vector2(0f, 6.9f);
        player.gameObject.SetActive(false);
        player.ResetPlayer();
        playerCorpse.gameObject.SetActive(false);
        player.Pause(false);

        //reset UI
        openButton.SetActive(true);

        gameOverAlpha.color = new Color(0, 0, 0, 0);
        GameOverPanel.gameObject.SetActive(false);

        menuCharacter.GetComponent<Animator>().Play("playerUI");
        menuCharacter.GetComponent<RectTransform>().DOAnchorPosX(0f, 0.0f, false).SetUpdate(true);
        gameOver = false;

        openButton.SetActive(false);
        countdownHourGlassUI.color = new Color(1, 1, 1, 0);

        // Reset timer
        timeCounter = 0;
        timer = 0;

        // parameters
        lastSpawnedSkill = new Skill[3];
        for (int i = 0; i < 3; i++)
        {
            lastSpawnedSkill[i] = Skill.ComboMaster;
        }

        yield return new WaitForSeconds(waitTime);
        levelText.color = new Color(1, 1, 1, 0);
        levelText.SetText("Level 1");
        musicText.color = new Color(1, 1, 1, 0);
        musicText.SetText("♪ -");
        timerText.color = new Color(1, 1, 1, 0);
        timerText.SetText(string.Empty);
        tipsText.SetText(string.Empty);
        comboText.SetText(string.Empty);
        countdownText.SetText(string.Empty);
        StartGame();
    }

    public bool IsLevelEnded()
    {
        return levelEnded;
    }

    public void SetCombo(int combo)
    {
        if (combo <= 0) return;
        comboText.SetText(combo.ToString());
        comboText.DOKill(false);
        comboText.color = new Color(1f, 1.1f - (0.1f * combo), 1.1f - (0.1f * combo), 1.0f);
        comboText.DOFade(0.0f, 5.0f);
        comboText.fontSize = 40f + (combo * 3f);
    }

    public void ResetCombo(int combo)
    {
        if (combo <= 0) return;
        comboText.SetText(combo + " Combo!");
        comboText.fontSize = comboText.fontSize + 3f;
        comboText.DOKill(false);
        comboText.color = new Color(1f, 1f, 1f, 1.0f);
        comboText.DOFade(0.0f, 5.0f);

        if (combo >= 15)
        {
            newGroundsAPI.NGUnlockMedal(65097);
        }
    }

    public bool IsSurvivorSelected()
    {
        return survivorSelected;
    }

    public void Survivorselected()
    {
        survivorSelected = true;
    }

    public bool IsPotionSelected()
    {
        return potionSelected;
    }

    public void PotionSelected()
    {
        potionSelected = true;

        // Open Potion UI
        itemUI.gameObject.SetActive(true);
        itemUI.GetComponent<CanvasGroup>().alpha = 0.0f;
        itemUI.GetComponent<CanvasGroup>().DOFade(1.0f, 1.0f);
        SetItemCooldownAmount(1.0f);
    }

    public void SetItemCooldownAmount(float value)
    {
        if (!itemUI.gameObject.activeSelf) return;

        itemUICooldown.DOFillAmount(value, 0.2f);
    }

    public void SkillSpawnedRecord(Skill skill, int index)
    {
        lastSpawnedSkill[index] = skill;
    }

    public bool IsSkillSpawnedLastLevel(Skill skill)
    {
        bool rtn = false;

        for (int i = 0; i < 3; i++)
        {
            if (lastSpawnedSkill[i] == skill) rtn = true;
        }

        return rtn;
    }

    public Color GetThemeColor()
    {
        return backgroundFrame.color;
    }

    public void SetPlayerName(string value)
    {
        playerName = value;
        FBPP.SetString("PlayerName", value);
        FBPP.Save();
    }

    // add extra time to timer
    IEnumerator AddTimerEffect(int addTime)
    {
        int newTimeLeft = addTime;
        isAddTimerEffectRunning = true;
        countdownHourGlassUI.DOColor(new Color(1, 1, 1, 0.25f), 0.1f);
        while (newTimeLeft > 0)
        {
            newTimeLeft--;

            timer = Mathf.Min(timer + 1, 120); // maximum 2 minutes
            UpdateCountdownTimerTextUI(timer);

            countdownText.color = new Color(countdownText.color.r, countdownText.color.g, countdownText.color.b, 1.0f);
            countdownHourGlassUI.GetComponent<RectTransform>().DOPunchPosition(new Vector3(0.1f, 0.1f, 0.1f), 0.1f);
            yield return new WaitForSeconds(0.1f);
        }
        countdownHourGlassUI.DOColor(new Color(1, 1, 1, 0.2f), 1.5f);
        countdownText.color = new Color(countdownText.color.r, countdownText.color.g, countdownText.color.b, 0.5f);
        isAddTimerEffectRunning = false;
    }

    IEnumerator LevelTimer(int level)
    {
        countdownHourGlassUI.DOColor(new Color(1, 1, 1, 0.2f), 4.0f);
        while (timer > 0)
        {
            if (isAddTimerEffectRunning) yield return new WaitForSeconds(1.0f);

            timer--;
            UpdateCountdownTimerTextUI(timer);

            if (timer < 20)
            {
                if (timer <= 5)
                {
                    AudioManager.Instance.PlaySFX("heartbeat");
                    countdownText.DOColor(Color.red, 0.1f);
                    countdownHourGlassUI.GetComponent<RectTransform>().DOPunchPosition(new Vector3(0.2f, 0.2f, 0.2f), 0.2f);
                }

                countdownText.DOFade(0.8f, 0.1f);
                countdownText.GetComponent<RectTransform>().DOScale(0.21f, 0.1f);
                yield return new WaitForSeconds(0.1f);

                if (timer <= 5)
                {
                    countdownText.DOColor(new Color(1.0f, 0.8f, 0.8f), 0.5f);
                }

                countdownText.DOFade(0.5f, 0.1f);
                countdownText.GetComponent<RectTransform>().DOScale(0.2f, 0.1f);
                yield return new WaitForSeconds(0.9f);
            }
            else
            {
                countdownText.DOComplete();
                Color newColor = GetThemeColor();
                countdownText.color = new Color(newColor.r, newColor.g, newColor.b, 0.5f);
                yield return new WaitForSeconds(1.0f);
            }

        }

        //int countdown;
        //for (countdown = 5; countdown > 0; countdown--)
        //{
        //    if (currentLevel <= level && !levelEnded)
        //    {
        //        countdownText.SetText(countdown.ToString());
        //        //countdownText.fontSize = 100;
        //        countdownText.DOFade(1.0f, 0.0f);
        //        countdownText.DOFade(0.0f, 0.8f);
        //        countdownText.GetComponent<RectTransform>().DOScale(1.0f, 0.0f);
        //        countdownText.GetComponent<RectTransform>().DOScale(0.0f, 0.8f);
        //        AudioManager.Instance.PlaySFX("heartbeat");
        //        yield return new WaitForSeconds(1.0f);
        //    }
        //}

        NarrativeText.SetText("<color=red>Time out!</color>");
        newGroundsAPI.NGUnlockMedal(65099);

        while (timer == 0)
        {
            if (currentLevel <= level && !levelEnded)
            {
                AudioManager.Instance.PlaySFX("burst");
                Instantiate(fireburstEffect, player.transform.position, Quaternion.identity);
                player.DealDamage(player.GetCurrentHP() / 2, player.transform);
                yield return new WaitForSeconds(2.0f);
            }
        }

        roundTimer = StartCoroutine(LevelTimer(level));
    }

    private void UpdateCountdownTimerTextUI(int time)
    {
        // convert second to time format
        int min = time / 60;
        int sec = time % 60;

        countdownText.SetText(min.ToString() + ":" + sec.ToString("00"));
    }

    IEnumerator TimeCounterLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            timeCounter++;

            // update UI
            timerText.SetText("Time - " + (timeCounter / 60).ToString() + ":" + (timeCounter % 60).ToString("00"));

            // update result manager
            ResultManager.Instance().SetTotalTime(timeCounter);
        }
    }

    private void SetBackground(string name)
    {
        if (name == string.Empty)
        {
            backgroundSprite.sprite = null;
            return;
        }

        backgroundSprite.sprite = Resources.Load<Sprite>("Background/" + name);
        backgroundSprite.color = new Color(GetThemeColor().r * 1.5f, GetThemeColor().g * 1.5f, GetThemeColor().b * 1.5f, 1.0f);
    }

    private void SetParallax(Transform parallaxParent, Color theme)
    {
        // disable all
        ResetParallax();

        parallaxParent.gameObject.SetActive(true);

        // active the chosen one
        if (parallaxParent.childCount == 0) return;

        for (int i = 0; i < parallaxParent.childCount; i++)
        {
            Transform child = parallaxParent.GetChild(i);
            if (child != null)
            {
                var component = child.GetComponent<SpriteRenderer>();
                component.color = new Color(theme.r, theme.g, theme.b, component.color.a);

                // child's child
                if (child.childCount > 0)
                {
                    for (int j = 0; j < child.childCount; j++)
                    {
                        Transform sub = child.GetChild(j);
                        if (sub != null)
                        {
                            sub.GetComponent<SpriteRenderer>().color = new Color(theme.r, theme.g, theme.b, sub.GetComponent<SpriteRenderer>().color.a);
                        }
                    }
                }
            }
        }
    }

    private void ResetParallax()
    {
        // disable all
        foreach (Transform parallax in parallaxList)
        {
            parallax.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (monsterList.Count == 0)
        {
            monsterAlertArrowLeft.DOFade(0.0f, 0.1f);
            monsterAlertArrowRight.DOFade(0.0f, 0.1f);
            return;
        }
        UpdateMonsterAlert();
    }

    private void UpdateMonsterAlert()
    {
        bool leftShow = false, rightShow = false;
        float posYLeft = -3.9f, posYRight = -0.39f;
        foreach (EnemyControl enemy in monsterList)
        {
            if (enemy.transform.position.x < -9f)
            {
                posYLeft = enemy.transform.position.y;
                leftShow = true;
            }
            if (enemy.transform.position.x > 9f)
            {
                posYRight = enemy.transform.position.y;
                rightShow = true;
            }
        }

        if (leftShow)
        {
            monsterAlertArrowLeft.DOFade(1.0f, 0.1f);
            monsterAlertArrowLeft.transform.position = new Vector2(monsterAlertArrowLeft.transform.position.x, posYLeft);
        }
        else
        {
            monsterAlertArrowLeft.DOFade(0.0f, 0.1f);
        }

        if (rightShow)
        {
            monsterAlertArrowRight.DOFade(1.0f, 0.1f);
            monsterAlertArrowRight.transform.position = new Vector2(monsterAlertArrowRight.transform.position.x, posYRight);
        }
        else
        {
            monsterAlertArrowRight.DOFade(0.0f, 0.1f);
        }
    }

    public string GetLeaderboardKey(LeaderboardType type)
    {
        string rtn;

        switch (type)
        {
            case LeaderboardType.Level:
                rtn = levelLeaderBoardID.ToString();
                break;
            case LeaderboardType.SpeedRunLevel10:
                rtn = SpeedRun10LeaderBoardID.ToString();
                break;
            case LeaderboardType.SpeedRunLevel20:
                rtn = SpeedRun20LeaderBoardID.ToString();
                break;
            default:
                rtn = string.Empty;
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }

        return rtn;
    }

    public bool IsConfirmKeyPressed()
    {
        return confrimKey;
    }

    public void ResetConfirmKey()
    {
        confrimKey = false;
    }

    int boolToInt(bool val)
    {
        if (val)
            return 1;
        else
            return 0;
    }

    bool intToBool(int val)
    {
        if (val != 0)
            return true;
        else
            return false;
    }
}
