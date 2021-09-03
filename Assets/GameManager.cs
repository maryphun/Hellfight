using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LootLocker.Requests;

public class GameManager : MonoBehaviour
{
    [SerializeField] float musicVolume = 0.7f;
    [SerializeField] GameObject floatingTextPrefab;
    [SerializeField] Image closeButton;
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
    [SerializeField] GameObject menuCanvas;
    [SerializeField] GameObject backCanvas;
    [SerializeField] string playerName;
    [SerializeField] TMP_Text countdownText;
    [SerializeField] TMP_Text gameoverText;
    [SerializeField] SpriteRenderer monsterAlertArrowLeft;
    [SerializeField] SpriteRenderer monsterAlertArrowRight;
    [SerializeField] string[] musicList;
    [SerializeField] Transform[] parallaxList;


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


    // Start is called before the first frame update
    void Awake()
    {
        DOTween.SetTweensCapacity(1250,50);
        monsterList = new List<EnemyControl>();
        fireburstEffect = Resources.Load("Prefabs/FireBurst") as GameObject;
    }

    public void Initialize()
    {
        allowRestart = false;
        backgroundFrame.color = new Color(0, 0, 0, 0);
        openButton.SetActive(false);
        lastSpawnedSkill = new Skill[3];
        for (int i = 0; i < 3; i++)
        {
            lastSpawnedSkill[i] = Skill.ComboMaster;
        }
    }

    public void SpawnFloatingText(Vector2 loc, float time, float _speed, string text, Color color, Vector2 direction, float fontSize)
    {
        var tmp = Instantiate(floatingTextPrefab);

        //Vector3 screenPointUnscaled = Camera.main.WorldToScreenPoint(loc);
        //Vector3 screenPointScaled = screenPointUnscaled / canvas.scaleFactor;

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
        screenAlpha.DOFade(1.0f, 0.0f);
        screenAlpha.DOFade(0.0f, 1.0f);
        menuCanvas.SetActive(false);
        backCanvas.SetActive(true);
        frontCanvas.gameObject.SetActive(true);
        SetStatusBarVisible(false);
        player.gameObject.SetActive(false);
        currentLevel = 1;
        StartCoroutine(StartGameCinematic());
    }

    IEnumerator StartGameCinematic()
    {
        yield return new WaitForSeconds(0.5f);
        NarrativeText.color = new Color(1.0f, 0.9f, 0.0f, 0.0f);
        NarrativeText.DOFade(1.0f, 2.0f);
        NarrativeText.SetText("Welcome our new challenger");
        backgroundFrame.DOColor(new Color(1, 1, 1, 1), 2.0f);
        AudioManager.Instance.PlaySFX("doon!");

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

        // level specific theme
        if (currentLevel == 10)
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
        levelText.SetText("Level " + currentLevel.ToString());
        musicText.DOFade(1.0f, 1.0f);
        StartLevel(currentLevel);

        // CHANGE BGM
        AudioManager.Instance.SetMusicVolume(musicVolume);
        lastMusic = RandomMusic();
        musicText.SetText("♪ - " + lastMusic);
        musicText.DOFade(1.0f, 1.0f);
        AudioManager.Instance.PlayMusic(lastMusic);
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
        roundTimer = StartCoroutine(LevelTimer(level, 50.0f));
        monsterSpawnCount = 0;

        if (level == 3)
        {
            tipsText.SetText("You move slower when you are low in stamina, make sure you rest!");
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 5)
        {
            tipsText.SetText("Your third combo attack deal more damage");
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 10)
        {
            tipsText.SetText("Head is his weakness!");
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 15)
        {
            tipsText.SetText("Beware of big wave of enemies!!");
            tipsText.DOFade(1.0f, 0.5f);
        }
        else if (level == 12)
        {
            tipsText.SetText("Your stamina gain slower as you dash continuously");
            tipsText.DOFade(1.0f, 0.5f);
        }
        else
        {
            tipsText.SetText(string.Empty);
            tipsText.DOFade(0.0f, 0.5f);
        }
    }

    private void EndLevel()
    {
        monsterSpawnCount = 0;
        monsterList.Clear();
        spawnEnded = false;
        levelEnded = true;
        StopCoroutine(roundTimer);
        NarrativeText.color = new Color(0.75f, 0.0f, 0.0f, 0.0f);
        NarrativeText.DOFade(1.0f, 0.5f);
        NarrativeText.SetText("Jump and proceed to the next level");
        if (currentLevel == 9) NarrativeText.SetText("Proceed to Boss Fight - <color=#ff00ffff>Broyon</color>!");
        if (currentLevel == 19) NarrativeText.SetText("Proceed to Boss Fight - <color=#550000ff>Hell Fighter</color>!");
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
            player.RegeneratePercentage(0.1f, 1.0f);
            openButton.SetActive(false);
            screenAlpha.DOFade(0.9f, 1.0f);
            collision.gameObject.SetActive(false);
            GameObject tmp = Instantiate(abilityLearnPanel, frontCanvas.transform);
            tmp.GetComponent<AbilityLearnPanel>().Initialize(currentLevel, this, player);
        }
    }

    public List<EnemyControl> GetMonsterList()
    {
        return monsterList;
    }

    public void OpenMenu()
    {
        AudioManager.Instance.PlaySFX("menuOpen");
        statusMenu.GetComponent<StatusMenuUpdate>().UpdateValue();
        screenAlpha.DOFade(0.8f, 0.25f).SetUpdate(true);
        statusMenu.GetComponent<RectTransform>().DOScaleX(1.0f, 0.25f).SetUpdate(true);
        float originalPos = closeButton.GetComponent<RectTransform>().anchoredPosition.x;
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, closeButton.GetComponent<RectTransform>().anchoredPosition.y);
        closeButton.GetComponent<RectTransform>().DOAnchorPosX(originalPos, 0.25f, false).SetUpdate(true);
        closeButton.DOFade(1.0f, 0.5f).SetUpdate(true);

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

    public void GameOver()
    {
        StopCoroutine(roundTimer);

        AudioManager.Instance.StopMusicWithFade(1.0f);
        openButton.SetActive(false);

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

        Time.timeScale = 0.0f; 
        gameOver = true;
        allowRestart = true;

        gameoverText.SetText("Uploading record to server...");

        int rank = UploadToLeaderBoard();
    }

    private int UploadToLeaderBoard()
    {
        int rank = -1;

        string memberID = "[name=" + playerName + "]";
        memberID += "[version=" + Application.version + "]";
        memberID += "[date=" + System.DateTime.Now.ToString("MM/dd") + "]";
        memberID += "[hp=" + player.GetMaxHP().ToString() + "]";
        memberID += "[hpregen=" + player.GetHPRegen().ToString() + "]";
        memberID += "[stamina=" + player.GetMaxStamina().ToString() + "]";
        memberID += "[movespeed=" + player.GetMoveSpeed().ToString() + "]";
        memberID += "[atkDmg=" + player.GetAttackDamage().ToString() + " ~ " + (player.GetAttackDamage()+player.GetMaxDamage()).ToString() + "]";
        memberID += "[dashDmg=" + player.GetDashDamage().ToString() + "]";
        memberID += "[dashCD=" + player.GetDashCD().ToString() + "]";
        memberID += "[critical=" + player.GetCritical().ToString() + "]";
        memberID += "[lifesteal=" + player.GetLifesteal().ToString() + "]";
        memberID += "[lifedrain=" + player.GetLifeDrain().ToString() + "]";
        memberID += "[combomaster=" + player.GetComboMaster().ToString() + "]";
        memberID += "[localtime=" + System.DateTime.Now.ToLocalTime().ToString() + "]";

        // record leaderboard
        LootLockerSDKManager.SubmitScore(memberID, currentLevel, 399, (response) =>
        {
            if (response.success)
            {
                rank = response.rank;

                gameoverText.SetText("You've ranked <color=#ffff00ff>#" + rank + "</color> in the global leaderboard.");

                if (rank == 1)
                {
                    gameoverText.SetText(gameoverText.text + "\nWhat a legend");
                }
                else if (rank <= 10)
                {
                    gameoverText.SetText(gameoverText.text + "\nYou're famous!");
                }
                else if (rank <= 20)
                {
                    gameoverText.SetText(gameoverText.text + "\nYou'll be remembered forever!");
                }
                else if (rank <= 30)
                {
                    gameoverText.SetText(gameoverText.text + "\nYour parent is proud of you!");
                }
            }
            else
            {
                gameoverText.SetText("Failed to connect server.");
                rank = -1;
            }
        });

        return rank;
    }

    public void RestartGame()
    {
        if (!allowRestart) return;
        allowRestart = false;
        screenAlpha.DOFade(1.0f, 0.5f).SetUpdate(true);
        Time.timeScale = 1.0f;
        StartCoroutine(RestartDelay(1.0f));
        AudioManager.Instance.PlaySFX("restart");
    }

    IEnumerator RestartDelay(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        //Scene scene = SceneManager.GetActiveScene();
        // SceneManager.LoadScene(scene.name);

        yield return new WaitForSeconds(waitTime);
        // initialize
        // Reset Color
        background.color = new Color(0, 0, 0, 1);
        backgroundFrame.color = new Color(0, 0, 0, 0);
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
        tipsText.SetText("");
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
    }

    public bool IsSurvivorSelected()
    {
        return survivorSelected;
    }

    public void Survivorselected()
    {
        survivorSelected = true;
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
        PlayerPrefs.SetString("PlayerName", value);
        PlayerPrefs.Save();
    }

    IEnumerator LevelTimer(int level, float time)
    {
        yield return new WaitForSeconds(time);

        int countdown;
        for (countdown = 5; countdown > 0; countdown--)
        {
            if (currentLevel <= level && !levelEnded)
            {
                countdownText.SetText(countdown.ToString());
                //countdownText.fontSize = 100;
                countdownText.DOFade(1.0f, 0.0f);
                countdownText.DOFade(0.0f, 0.8f);
                countdownText.GetComponent<RectTransform>().DOScale(1.0f, 0.0f);
                countdownText.GetComponent<RectTransform>().DOScale(0.0f, 0.8f);
                AudioManager.Instance.PlaySFX("heartbeat");
                yield return new WaitForSeconds(1.0f);
            }
        }

        NarrativeText.SetText("<color=red>You've over stayed</color>");

        while (currentLevel <= level && !levelEnded)
        {
            AudioManager.Instance.PlaySFX("burst");
            Instantiate(fireburstEffect, player.transform.position, Quaternion.identity);
            player.DealDamage(player.GetCurrentHP() / 2, player.transform);
            yield return new WaitForSeconds(2.0f);
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
    }

    private void SetParallax(Transform parallaxParent, Color theme)
    {
        // disable all
        ResetParallax();

        // active the chosen one
        Transform far = parallaxParent.GetChild(0);
        Transform mid = parallaxParent.GetChild(1);
        Transform close = parallaxParent.GetChild(2);

        parallaxParent.gameObject.SetActive(true);

        far.GetComponent<SpriteRenderer>().color = new Color(theme.r, theme.g, theme.b, 0.1f);
        mid.GetComponent<SpriteRenderer>().color = new Color(theme.r, theme.g, theme.b, 0.5f);
        close.GetComponent<SpriteRenderer>().color = new Color(theme.r, theme.g, theme.b, 1f);
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
}
