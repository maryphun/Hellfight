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
    [SerializeField] int levelLeaderBoardID = 402;
    [SerializeField] int SpeedRun10LeaderBoardID = 402;
    [SerializeField] int SpeedRun20LeaderBoardID = 402;
    [SerializeField] int cheaterLeaderboardID = 434;
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
    [SerializeField] GameObject hpBar;
    [SerializeField] GameObject staminaBar;
    [SerializeField] GameObject hpBarText;
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
    [SerializeField] TMP_Text gameoverText;
    [SerializeField] SpriteRenderer monsterAlertArrowLeft;
    [SerializeField] SpriteRenderer monsterAlertArrowRight;
    [SerializeField] Transform playerCorpse;
    [SerializeField] RectTransform itemUI;
    [SerializeField] Image itemUICooldown;
    [SerializeField] GameObject newUnlockMenu;
    [SerializeField] TMP_Text newUnlockName;
    [SerializeField] Image newUnlockIcon;
    [SerializeField] Image newUnlockAlpha;
    [SerializeField] NewGroundAPI newGroundsAPI;
    [SerializeField] string[] musicList;
    [SerializeField] string[] bossMusicList;
    [SerializeField] Transform[] parallaxList;

    [Header("Game Configuaration")]
    [SerializeField] int initialTime = 50;
    [SerializeField] int extraTimePerLevel = 15;

    [Header("Debug")]
    [SerializeField] int currentLevel;
    [SerializeField] int totalNumberOfMonster;
    [SerializeField] List<EnemyControl> monsterList;
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
    List<Skill> newUnlock = new List<Skill>();
    public PlayerAction _input;
    bool confrimKey = false;
    bool isRoundTimerRunning = false;
    bool isAddTimerEffectRunning = false;

    int timer;

    // Start is called before the first frame update
    void Awake()
    {
        DOTween.SetTweensCapacity(1250,50);
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
    }

    public void SpawnFloatingText(Vector2 loc, float time, float _speed, string text, Color color, Vector2 direction, float fontSize)
    {
        var tmp = Instantiate(floatingTextPrefab);
        tmp.transform.localPosition = loc;
        tmp.GetComponent<floaitngtext>().Initialize(time, _speed, text, color, direction, fontSize);
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

    public void StartGame()
    {
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

        ProgressManager.Instance().LoadUnlockLevel();
        StartCoroutine(StartGameCinematic());
    }

    IEnumerator StartGameCinematic()
    {
        yield return new WaitForSeconds(0.5f);
        NarrativeText.color = new Color(1.0f, 0.9f, 0.0f, 0.0f);
        NarrativeText.DOFade(1.0f, 2.0f);

        string narrative = LocalizationManager.Localize("Text.NewChallenger");
        if (ProgressManager.Instance().GetUnlockLevel() > 0)
        {
            narrative = LocalizationManager.Localize("Text.Retry");
        }
        NarrativeText.SetText(narrative);

        backgroundFrame.DOColor(new Color(1, 1, 1, 1), 2.0f);
        AudioManager.Instance.PlaySFX("doon!");

        // check what player use as a input method by detecting his next input and get a call back
        UnityEvent tmp = new UnityEvent();
        tmp.AddListener(InputDetected);
        ControlPattern.Instance().DetectControlPattern(tmp);

        yield return new WaitForSeconds(2.0f);
        StartLevelCinematic();

        yield return new WaitForSeconds(0.2f);
        SetStatusBarVisible(true);
        StartLevel(currentLevel);

        yield return new WaitForSeconds(2.0f);
        levelText.DOFade(1.0f, 1.0f);
    }

    public void SetStatusBarVisible(bool boolean)
    {
        hpBar.SetActive(boolean);
        staminaBar.SetActive(boolean);
        hpBarText.SetActive(boolean);
        staminaBarText.SetActive(boolean);

        // only show item UI if potion is available
        if (IsPotionSelected() || !boolean)
        {
            itemUI.gameObject.SetActive(boolean);
        }
    }

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
    public void TimerAddTimeNewLevel()
    {
        TimerAddTime(extraTimePerLevel);
    }

    public void ScreenImpactGround(float time, float magnitude)
    {
        Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - magnitude, Camera.main.transform.position.z);
        StartCoroutine(ReturnScreenPosition(time));
    }

    public void ScreenChangeTheme()
    {
        if (currentLevel == 1) return;

        // one of the color must be 255 to make sure it's not a dark color
        int rand = Random.Range(0, 3);
        Color newColor = new Color();
        switch (rand)
        {
            case 0:
                newColor = new Color(1.0f, Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
                break;
            case 1:
                newColor = new Color(Random.Range(0.0f, 1.0f), 1.0f, Random.Range(0.0f, 1.0f));
                break;
            case 2:
                newColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);
                break;
            default:
                newColor = new Color(0.0f, 0.0f, 0.0f);
                break;
        }

        backgroundFrame.color = newColor;
        background.color = new Color(newColor.r / 10f, newColor.g / 10f, newColor.b / 10f, 1.0f);
        backgroundSprite.color = new Color(newColor.r / 10f, newColor.g / 10f, newColor.b / 10f, 1.0f);
        musicText.color = newColor;
        levelText.color = newColor;
        timerText.color = newColor;
        countdownText.color = new Color(newColor.r, newColor.g, newColor.b, 0.5f); ;

        // level specific theme
        if (currentLevel == 5)
        {
            backgroundSprite.GetComponent<Animator>().enabled = true;
            backgroundSprite.color = new Color(GetThemeColor().r * 0.8f, GetThemeColor().g * 0.8f, GetThemeColor().b * 0.8f, 1.0f);
            ResetParallax();
        }
        else if (currentLevel == 6)
        {
            backgroundSprite.GetComponent<Animator>().enabled = false;
            SetBackground(string.Empty);
            SetParallax(RandomParallax(), newColor);
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
        else
        {
            SetBackground(string.Empty);
            SetParallax(RandomParallax(), newColor);
        }
    }

    IEnumerator ReturnScreenPosition(float delay)
    {
        yield return new WaitForSeconds(delay);

        Camera.main.transform.position = new Vector3(0, 0, -10);
    }

    public void StartLevelCinematic()
    {
        screenAlpha.DOFade(0.0f, 1.0f);
        NarrativeText.SetText(string.Empty);
        player.gameObject.SetActive(true);
        player.transform.position = new Vector2(Mathf.Clamp(player.transform.position.x, - 8f, 8f), 6.9f);
        player.GetComponent<Rigidbody2D>().velocity = new Vector2(0.0f, -60f);
    }

    public void NextLevel()
    {
        currentLevel++;

        // PLAYER DIE HERE LAST TIME?
        playerCorpse.gameObject.SetActive(false);
        if (ProtectedPlayerPrefs.GetInt("LastDeath", -1) == currentLevel)
        {
            // player die on here last time
            playerCorpse.position = new Vector2(ProtectedPlayerPrefs.GetFloat("LastDeathPosition", 0.0f), playerCorpse.position.y);
            playerCorpse.GetComponent<SpriteRenderer>().flipX = intToBool(ProtectedPlayerPrefs.GetInt("LastDeathFlip", 0));
            playerCorpse.gameObject.SetActive(true);

            PlayerPrefs.DeleteKey("LastDeath");
            PlayerPrefs.DeleteKey("LastDeathPosition");
            PlayerPrefs.DeleteKey("LastDeathFlip");
            PlayerPrefs.Save();
        }

        // LEVEL TEXT
        levelText.SetText("Level " + currentLevel.ToString());
        musicText.DOFade(1.0f, 1.0f);
        if (ProtectedPlayerPrefs.GetInt("ShowTimer", 0) == 1) timerText.DOFade(1.0f, 1.0f);
        StartLevel(currentLevel);

        // CHANGE BGM
        AudioManager.Instance.SetMusicVolume(musicVolume);
        lastMusic = RandomMusic();
        if (currentLevel % 10 == 0)
        {
            // boss music
            lastMusic = bossMusicList[(currentLevel / 10)-1];
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
        if (currentLevel == 26)
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

        for (int i = 0; i < musicList.Length; i++)   if (musicList[i] != lastMusic) list.Add(i);

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
        int record = ProgressManager.Instance().GetUnlockLevel();
        if (level == 1 && (record <= level))
        {
            if (ControlPattern.Instance().IsJoystickConnected())
            {
                tipsText.SetText(Input.GetJoystickNames()[0] + "\n<font=pixelinput SDF>4</font> "+ LocalizationManager.Localize("Tutorial.Attack") + " <font=pixelinput SDF>6</font> " + LocalizationManager.Localize("Tutorial.Jump") + " <font=pixelinput SDF>7</font> " + LocalizationManager.Localize("Tutorial.Dash"));
            }
            else
            {
                tipsText.SetText("<font=pixelinput SDF>W</font>\n<font=pixelinput SDF>ASD</font>\n" + LocalizationManager.Localize("Tutorial.Move") + "\n<font=pixelinput SDF>z</font> " + LocalizationManager.Localize("Tutorial.Attack") + " <font=pixelinput SDF>x</font> " + LocalizationManager.Localize("Tutorial.Jump"));
            }
            tipsText.DOFade(1.0f, 0.5f);
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
        else if (level == 11 && record <= level)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.TipsLevel11"));
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
        else if (level == 26)
        {
            tipsText.SetText(LocalizationManager.Localize("Tutorial.TipsLevel26"));
            tipsText.DOFade(1.0f, 0.5f);
        }
        else  if (potionSelected && ProtectedPlayerPrefs.GetInt("TutorialPotion", 0) != 1)
        {
            ProtectedPlayerPrefs.SetInt("TutorialPotion", 1);
            if (ControlPattern.Instance().IsJoystickConnected())
            {
                tipsText.SetText("\n<font=pixelinput SDF>+</font> " + LocalizationManager.Localize("Tutorial.UseItem"));
            }
            else
            {
                tipsText.SetText("<font=pixelinput SDF>c</font> " + LocalizationManager.Localize("Tutorial.UseItem"));
            }
        }
        else
        {
            tipsText.SetText(string.Empty);
            tipsText.DOFade(0.0f, 0.5f);
        }
    }

    private void InputDetected()
    {
        int record = ProgressManager.Instance().GetUnlockLevel();
        if (currentLevel == 1 && record <= currentLevel)
        {
            if (ControlPattern.Instance().GetControlPattern() == ControlPattern.CtrlPattern.JOYSTICK)
            {
                tipsText.SetText(Input.GetJoystickNames()[0] + "\n<font=pixelinput SDF>4</font> " + LocalizationManager.Localize("Tutorial.Attack") + " <font=pixelinput SDF>6</font> " + LocalizationManager.Localize("Tutorial.Jump") + " <font=pixelinput SDF>7</font> " + LocalizationManager.Localize("Tutorial.Dash"));
            }
            else
            {
                tipsText.SetText("<font=pixelinput SDF>W</font>\n<font=pixelinput SDF>ASD</font>\n" + LocalizationManager.Localize("Tutorial.Move") + "\n<font=pixelinput SDF>z</font> " + LocalizationManager.Localize("Tutorial.Attack") + " <font=pixelinput SDF>x</font> " + LocalizationManager.Localize("Tutorial.Jump"));
            }
        }
    }

    private void EndLevel()
    {
        monsterSpawnCount = 0;
        monsterList.Clear();
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
        if (LocalizationManagerHellFight.Instance().GetCurrentLanguage() == "Japanese") NarrativeText.SetText("<font=JPPixel SDF>" + NarrativeText.text + "</font>");
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
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Controller>() != null)
        {
            player.Regenerate(player.GetHPRegen()/* + (player.GetMaxHP() / 10)*/, player.GetStaminaMax(), false);
            itemUICooldown.fillAmount = 1f;
            openButton.SetActive(false);
            screenAlpha.DOFade(0.9f, 1.0f);
            collision.gameObject.SetActive(false);
            GameObject tmp = Instantiate(abilityLearnPanel, frontCanvas.transform);
            tmp.GetComponent<AbilityLearnPanel>().Initialize(currentLevel, this, player);

            // SPEED RUNNER
            if (currentLevel == 10)
            {
                float besttime = ProtectedPlayerPrefs.GetFloat("SpeedRunLevel10", 7200f);

                if (besttime > timeCounter)
                {
                    ProtectedPlayerPrefs.SetFloat("SpeedRunLevel10", timeCounter);
                }
            }
            if (currentLevel == 20)
            {
                float besttime = ProtectedPlayerPrefs.GetFloat("SpeedRunLevel20", 7200f);
                
                if (besttime > timeCounter)
                {
                    ProtectedPlayerPrefs.SetFloat("SpeedRunLevel20", timeCounter);
                }
            }
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
        foreach(EnemyControl enemy in monsterList)
        {
            enemy.Pause(true);
        }

        AudioManager.Instance.PauseMusic();
        Time.timeScale = 0.0f;
    }

    public void CloseMenu()
    {
        AudioManager.Instance.PlaySFX("menuClose");
        Time.timeScale = 1.0f;

        screenAlpha.DOFade(0.0f, 0.25f).SetUpdate(true);
        statusMenu.GetComponent<RectTransform>().DOScaleX(0.0f, 0.25f).SetUpdate(true);
        closeButton.DOFade(0.0f, 0.0f).SetUpdate(true);
        timerButton.DOFade(0.0f, 0.0f).SetUpdate(true);

        // pause game
        player.Pause(false);
        foreach (EnemyControl enemy in monsterList)
        {
            enemy.Pause(false);
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

        ProtectedPlayerPrefs.SetInt("ShowTimer", timerText.gameObject.activeSelf ? 1 : 0);
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

        // UI
        GameOverPanel.GetComponent<RectTransform>().DOScaleX(1.0f, 0.5f).SetUpdate(true);

        gameOverAlpha.DOFade(0.8f, 0.5f).SetUpdate(true);

        leaderBoardButton.gameObject.SetActive(true);
        leaderBoardButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, leaderBoardButton.GetComponent<RectTransform>().anchoredPosition.y);
        leaderBoardButton.GetComponent<RectTransform>().DOAnchorPosX(110f, 0.5f, false).SetUpdate(true);
        leaderBoardButton.DOFade(1.0f, 1.0f).SetUpdate(true);

        StatusButton.gameObject.SetActive(true);
        StatusButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, StatusButton.GetComponent<RectTransform>().anchoredPosition.y);
        StatusButton.GetComponent<RectTransform>().DOAnchorPosX(-110f, 0.5f, false).SetUpdate(true);
        StatusButton.DOFade(1.0f, 1.0f).SetUpdate(true);

        restartButton.gameObject.SetActive(true);

        menuCharacter.GetComponent<Animator>().Play("playerUIDead");
        menuCharacter.GetComponent<RectTransform>().DOAnchorPosX(27f, 0.0f, false).SetUpdate(true);

        // PAUSE GAME
        Time.timeScale = 0.0f; 

        // FLAG
        gameOver = true;
        allowRestart = true;

        // SAVE LOCAL DATA
        ProtectedPlayerPrefs.SetInt("LastDeath", currentLevel);
        ProtectedPlayerPrefs.SetFloat("LastDeathPosition", player.transform.position.x);
        ProtectedPlayerPrefs.SetInt("LastDeathFlip", boolToInt(player.IsFlip()));
        PlayerPrefs.Save(); 
        
        newUnlock = ProgressManager.Instance().NewStuffUnlocked(currentLevel);

        // SUBMIT NEWGROUNDS SCOREBOARD
        newGroundsAPI.NGSubmitScore(10762, currentLevel);

        // SUBMIT LOOTLOCKER SCOREBOARD
        gameoverText.SetText("Uploading record to server...");
        int rank = UploadToLeaderBoard(LeaderboardType.Level);
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
        int leaderboardID;
        int data;
        switch (type)
        {
            case LeaderboardType.Level:
                leaderboardID = GetLeaderboardID(type);
                data = currentLevel;
                break;
            case LeaderboardType.SpeedRunLevel10:
                leaderboardID = SpeedRun10LeaderBoardID;
                data = Mathf.RoundToInt(ProtectedPlayerPrefs.GetFloat("SpeedRunLevel10", 7200f));
                break;
            case LeaderboardType.SpeedRunLevel20:
                leaderboardID = SpeedRun20LeaderBoardID;
                data = Mathf.RoundToInt(ProtectedPlayerPrefs.GetFloat("SpeedRunLevel20", 7200f));
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                data = 0;
                leaderboardID = 0;
                return rank;
                break;
        }

        // record leaderboard
        LootLockerSDKManager.SubmitScore(memberID, data, leaderboardID, (response) =>
        {
            if (response.success)
            {
                rank = response.rank;
                data = response.score;

                SetGameOverText(type, rank, data);

                if (currentLevel > 10 && type == LeaderboardType.Level) UploadToLeaderBoard(LeaderboardType.SpeedRunLevel10);
                if (currentLevel > 20 && type == LeaderboardType.SpeedRunLevel10) UploadToLeaderBoard(LeaderboardType.SpeedRunLevel20);
            }
            else
            {
                gameoverText.SetText("Failed to connect server.");
                rank = -1;
            }
        });

        return rank;
    }

    private void SetGameOverText(LeaderboardType type, int rank, int data)
    {
        int min, sec;
        switch (type)
        {
            case LeaderboardType.Level:
                gameoverText.SetText("You've ranked <color=#ffff00ff>#" + rank.ToString() + "</color> in the global leaderboard. (level " + data.ToString() + ")");
                break;
            case LeaderboardType.SpeedRunLevel10:
                min = data / 60;
                sec = data % 60;
                gameoverText.SetText(gameoverText.text + "\n<color=#ffff00ff>#" + rank.ToString() + "</color> fastest person to beat level 10. (" + min.ToString() + "m" + sec.ToString() + "s)");
                break;
            case LeaderboardType.SpeedRunLevel20:
                min = data / 60;
                sec = data % 60;
                gameoverText.SetText(gameoverText.text + "\n<color=#ffff00ff>#" + rank.ToString() + "</color> fastest person to beat level 20. (" + min.ToString() + "m" + sec.ToString() + "s)");
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }
    }

    public void RestartGame()
    {
        if (!allowRestart) return;
        allowRestart = false;
        screenAlpha.DOFade(1.0f, 0.5f).SetUpdate(true);
        AudioManager.Instance.PlaySFX("restart");

        // determine need to enter new unlock phase or not
        if (newUnlock.Count > 0)
        {
            // reset input
            confrimKey = false;
            StartCoroutine(NewUnlockMenuPhase(RestartDelay(0.5f)));
        }
        else
        {
            // there are no new unlock. restart the game.
            Time.timeScale = 1.0f;
            StartCoroutine(RestartDelay(2.0f));
        }
    }

    IEnumerator NewUnlockMenuPhase(IEnumerator nextPhase)
    {
        yield return new WaitForSecondsRealtime(1.0f);
        newUnlockAlpha.DOFade(1.0f, 0.0f).SetUpdate(true);
        // CHECK IF NEW STUFF UNLOCKED
        newUnlockMenu.SetActive(true);

        bool showMenu = true;
        while (showMenu)
        {
            newUnlockAlpha.DOFade(0.0f, 0.25f).SetUpdate(true);
            AudioManager.Instance.PlaySFX("Unlock", 0.85f);
            SelectionData data = ProgressManager.Instance().EnumToData(newUnlock[0]);
            newUnlockIcon.sprite = Resources.Load<Sprite>("Icon/" + data.skill_Icon);
            newUnlockName.SetText(data.skill_name);

            while (!confrimKey)
            {
                Debug.Log("waiting for input" + confrimKey);
                yield return null;
            }
            Debug.Log("YESS!" + confrimKey);
            confrimKey = false; // reset input

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

        Time.timeScale = 1.0f;
        StartCoroutine(nextPhase);
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

        // Reset Player
        player.transform.position = new Vector2(0f, 6.9f);
        player.gameObject.SetActive(false);
        player.ResetPlayer();
        playerCorpse.gameObject.SetActive(false);

        //reset UI
        openButton.SetActive(true);

        gameOverAlpha.color = new Color(0, 0, 0, 0);
        GameOverPanel.GetComponent<RectTransform>().DOScaleX(0.0f, 0.0f);
        restartButton.gameObject.SetActive(false);
        StatusButton.gameObject.SetActive(false);
        leaderBoardButton.gameObject.SetActive(false);

        menuCharacter.GetComponent<Animator>().Play("playerUI");
        menuCharacter.GetComponent<RectTransform>().DOAnchorPosX(0f, 0.0f, false).SetUpdate(true);
        gameOver = false;

        openButton.SetActive(false);


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
        ProtectedPlayerPrefs.SetString("PlayerName", value);
        PlayerPrefs.Save();
    }

    // add extra time to timer
    IEnumerator AddTimerEffect(int addTime)
    {
        int newTimeLeft = addTime;
        isAddTimerEffectRunning = true;
        while (newTimeLeft > 0)
        {
            newTimeLeft--;

            timer++;
            UpdateCountdownTimerTextUI(timer);

            countdownText.color = new Color(countdownText.color.r, countdownText.color.g, countdownText.color.b, 1.0f);
            yield return new WaitForSeconds(0.1f);
        }
        countdownText.color = new Color(countdownText.color.r, countdownText.color.g, countdownText.color.b, 0.5f);
        isAddTimerEffectRunning = false;
    }

    IEnumerator LevelTimer(int level)
    {
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
            timerText.SetText("Time - " + (timeCounter / 60).ToString() + ":" + (timeCounter % 60).ToString("00"));
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
        backgroundSprite.color = new Color(GetThemeColor().r * 0.8f, GetThemeColor().g * 0.8f, GetThemeColor().b * 0.8f, 1.0f);
    }

    private void SetParallax(Transform parallaxParent, Color theme)
    {
        // disable all
        ResetParallax();

        parallaxParent.gameObject.SetActive(true);

        // active the chosen one
        if (parallaxParent.childCount == 0) return;

        Transform far = parallaxParent.GetChild(0);
        if (far != null) far.GetComponent<SpriteRenderer>().color = new Color(theme.r, theme.g, theme.b, 0.1f);

        if (parallaxParent.childCount < 2) return;

        Transform mid = parallaxParent.GetChild(1);
        if (mid != null) mid.GetComponent<SpriteRenderer>().color = new Color(theme.r, theme.g, theme.b, 0.5f);

        if (parallaxParent.childCount < 3) return;

        Transform close = parallaxParent.GetChild(2);
        if (close != null) close.GetComponent<SpriteRenderer>().color = new Color(theme.r, theme.g, theme.b, 1f);
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

    public int GetLeaderboardID(LeaderboardType type)
    {
        int rtn;

        switch (type)
        {
            case LeaderboardType.Level:
                rtn = levelLeaderBoardID;
                break;
            case LeaderboardType.SpeedRunLevel10:
                rtn = SpeedRun10LeaderBoardID;
                break;
            case LeaderboardType.SpeedRunLevel20:
                rtn = SpeedRun20LeaderBoardID;
                break;
            default:
                rtn = -1;
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }

        return rtn;
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
