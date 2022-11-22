using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using LootLocker.Requests;
using OPS.AntiCheat.Prefs;

public enum LeaderboardType
{
    Level,
    SpeedRunLevel10,
    SpeedRunLevel20,

    Min = Level,
    Max = SpeedRunLevel20,
}

public class Menu : MonoBehaviour
{
    enum MenuState
    {
        NONE,
        START,
        LANGUAGE_SELECT,
        NAME_INPUT,
        MAIN_MENU,
        LEADERBOARD,
    }

    enum MenuSelection
    {
        MainGame,
        BossRushMode,
        Leaderboard,
        Option,
        ResetLocalData,
        Exit,

        MaxIndex
    }

    enum LanguageSelection
    {
        English,
        SimplifiedChinese,
        TraditionalChinese,
        Japanese,

        MaxIndex
    }

    [SerializeField] Animator pageAnimator;
    [SerializeField] Image frame;
    [SerializeField] Image background;
    [SerializeField] TMP_Text logo;
    [SerializeField] Image leaderboardBack;
    [SerializeField] TMP_Text versionText;
    [SerializeField] GameObject copyrightUI;
    [SerializeField] Image menuAlpha;
    [SerializeField] GameManager gameMng;
    [SerializeField] TMP_InputField playerNameText;
    [SerializeField] RectTransform playerNameText_UI;
    [SerializeField] Leaderboard leaderboardObject;
    [SerializeField] RectTransform resetDataPanel;
    [SerializeField] TMP_Text resetDataText;
    [SerializeField] GameObject startGameText;
    [SerializeField] RectTransform mainMenuUI;
    [SerializeField] RectTransform mainPageUI;
    [SerializeField] RectTransform leaderboardPageUI;
    [SerializeField] RectTransform languageSelectionParent;
    [SerializeField] RectTransform selectIcon;
    [SerializeField] RectTransform languageSelectIcon;
    [SerializeField] TMP_Text[] choiceText;
    [SerializeField] TMP_Text[] languageChoiceText;
    [SerializeField] TMP_Text highestRecordLevelText;

    [Header("Debug")]
    [SerializeField] MenuSelection selectIndex;
    [SerializeField] LanguageSelection languageSelectIndex;
    [SerializeField] MenuState menuState;
    [SerializeField] LeaderboardType leaderboardType;

    bool disableMenuControl = false;
    bool leaderboardclickable = false;

    [SerializeField] Dictionary<string, string>[] leaderboardRankList;
    int leaderBoardTotalEntry;
    public PlayerAction _input;

    private void Start()
    {
        // UI
        selectIndex = MenuSelection.MainGame;
        disableMenuControl = true;
        leaderboardclickable = true;

        // INITIALIZE LOCALIZATION
        string language = ProtectedPlayerPrefs.GetString("Language", string.Empty);
        LocalizationManagerHellFight.Instance().Initialization(Application.systemLanguage);
        LocalizationManagerHellFight.Instance().SetCurrentLanguage(language);

        // LOCAL NAME SAVED
        playerNameText.text = ProtectedPlayerPrefs.GetString("PlayerName", string.Empty); ;

        // UPDATE VERSION NAME
        versionText.SetText("ver. " + Application.version + "(beta)");

        // CONNECT TO LEADERBOARD
        LootLockerSDKManager.StartSession("Player", (response) =>
        {
            if (response.success)
            {
                leaderboardRankList = new Dictionary<string, string>[30];
                for (int i = 0; i < 30; i++)
                {
                    leaderboardRankList[i] = new Dictionary<string, string>();
                }
                leaderboardType = LeaderboardType.Level;
                if (ProgressManager.Instance().LoadUnlockLevel() > 10 ) leaderboardType = LeaderboardType.SpeedRunLevel10;
                if (ProgressManager.Instance().LoadUnlockLevel() > 20 ) leaderboardType = LeaderboardType.SpeedRunLevel20;
                GetLeaderboardData(leaderboardType);
            }
        });

        // MUSIC MANAGER
        AudioManager.Instance.SetMusicVolume(0.7f);
        AudioManager.Instance.SetSEMasterVolume(0.25f);

        // FADE IN
        menuAlpha.DOFade(0.0f, 2.0f);

        // INIT
        menuState = MenuState.START;
    }

    private void Awake()
    {
        // SCREEN
#if UNITY_ANDROID
        if (Screen.height / Screen.width < 1.5)
        {
            Screen.SetResolution(1440, 810, true);
        }
#endif

        // INPUT SYSTEM
        _input = new PlayerAction();
        _input.MenuControls.Move.performed += ctx => SelectUpDown(ctx.ReadValue<float>());
        _input.MenuControls.AnyKey.performed += ctx =>   StartMenu();
        _input.MenuControls.Confirm.performed += ctx =>  FinishNameInput();
        _input.MenuControls.Confirm.performed += ctx =>  SelectSelection();
        _input.MenuControls.Cancel.performed += ctx =>   HideLeaderBoard();
    }

    private void OnEnable()
    {
        _input.Enable();
    }

    private void OnDisable()
    {
        _input.Disable();
    }

    private void StartMenu()
    {
        if (menuState != MenuState.START) return;

        menuState = MenuState.NONE;
        AudioManager.Instance.PlaySFX("startgame");

        const float animTime = 0.5f;
        RectTransform component = startGameText.GetComponent<RectTransform>();

        component.DORotate(new Vector3(0.0f, 0.0f, -360.0f), animTime, RotateMode.FastBeyond360);

        StartCoroutine(StartMenuAnimation(component, new Vector2(-391.0f, -265.0f), animTime));
        copyrightUI.GetComponent<RectTransform>().DOLocalMoveX(465.0f, animTime);
    }

    private void FinishNameInput()
    {
        if (menuState != MenuState.NAME_INPUT) return;
        if (playerNameText.text.Length <= 0) return;

        // DISABLE MENU INPUT
        menuState = MenuState.NONE;
        // DISABLE NAME INPUT
        playerNameText.DeactivateInputField();
        playerNameText.interactable = false;
        playerNameText.ReleaseSelection();
        // SET TO GAME MANAGER
        gameMng.SetPlayerName(playerNameText.text);
        // SE
        AudioManager.Instance.PlaySFX("startgame");
        // ANIMATION
        StartCoroutine(StartMenuAnimation(playerNameText_UI, new Vector2(-391.0f, -265.0f), 1.5f));
    }

    IEnumerator StartMenuAnimation(RectTransform obj, Vector2 endPos, float animTime)
    {
        // play animation as requested
        Vector3 originalPos = obj.position;
        obj.DOLocalMove(endPos, animTime);
        yield return new WaitForSeconds(animTime);

        // return to original position and hide it.
        obj.gameObject.SetActive(false);
        obj.position = originalPos;

        string language = ProtectedPlayerPrefs.GetString("Language", string.Empty);
        playerNameText.text = ProtectedPlayerPrefs.GetString("PlayerName", string.Empty);

        if (language == string.Empty)
        {
            menuState = MenuState.LANGUAGE_SELECT;
        }
        else if (playerNameText.text == string.Empty)
        {
            menuState = MenuState.NAME_INPUT;
        }
        else
        {
            menuState = MenuState.MAIN_MENU;
        }

        switch (menuState)
        {
            case MenuState.LANGUAGE_SELECT:
                languageSelectionParent.gameObject.SetActive(true);
                languageSelectIndex = GetDefaultLanguageSelection();

                // Default selection choice
                ChangeLanguageSelection(languageSelectIndex, 160f);
                break;
            case MenuState.MAIN_MENU:
                // ACTIVE UI COMPONENT
                logo.gameObject.SetActive(true);
                mainMenuUI.gameObject.SetActive(true);
                selectIndex = MenuSelection.MainGame;
                // SE
                AudioManager.Instance.PlaySFX("decide");
                AudioManager.Instance.PlayMusic("Battle-Sanctuary");

                // FLAG
                disableMenuControl = false;

                // Default selection choice
                ChangeSelection(MenuSelection.MainGame, 107.375f);

                // Setup Text
                SetupMainMenuUI();
                break;
            case MenuState.NAME_INPUT:
                playerNameText.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
                playerNameText_UI.gameObject.SetActive(true);
                playerNameText.interactable = true;
                playerNameText.Select();
                break;
        }
        
    }

    private LanguageSelection GetDefaultLanguageSelection()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.English:
                return LanguageSelection.English;
            case SystemLanguage.ChineseSimplified:
                return LanguageSelection.SimplifiedChinese;
            case SystemLanguage.ChineseTraditional:
                return LanguageSelection.TraditionalChinese;
            case SystemLanguage.Japanese:
                return LanguageSelection.Japanese;
        }
        return LanguageSelection.English;
    }

    private void ChangeSelection(MenuSelection index, float offset = -1)
    {
        selectIcon.anchoredPosition = new Vector2(0f, choiceText[(int)index].GetComponent<RectTransform>().anchoredPosition.y);

        RectTransform leftIcon = selectIcon.GetChild(0).GetComponent<RectTransform>();
        RectTransform rightIcon = selectIcon.GetChild(1).GetComponent<RectTransform>();

        float size = offset == -1 ? choiceText[(int)index].textBounds.size.x : offset;
        float position = choiceText[(int)index].GetComponent<RectTransform>().anchoredPosition.x;

        leftIcon.anchoredPosition = new Vector2(-size / 2f - 20f + position, leftIcon.anchoredPosition.y);
        rightIcon.anchoredPosition = new Vector2(size / 2f + 20f + position, rightIcon.anchoredPosition.y);

        for (int i = 0; i < (int)MenuSelection.MaxIndex ;i++)
        {
            if (i == (int)index)
            {
                choiceText[i].color = new Color(0.4f, 0.0f, 0.0f);
            }
            else
            {
                choiceText[i].color = Color.black;
            }
        }
    }

    private void ChangeLanguageSelection(LanguageSelection index, float offset = -1)
    {
        languageSelectIcon.anchoredPosition = new Vector2(0f, languageChoiceText[(int)index].GetComponent<RectTransform>().anchoredPosition.y);

        RectTransform leftIcon = languageSelectIcon.GetChild(0).GetComponent<RectTransform>();
        RectTransform rightIcon = languageSelectIcon.GetChild(1).GetComponent<RectTransform>();

        float size = offset == -1 ? languageChoiceText[(int)index].textBounds.size.x : offset;

        leftIcon.anchoredPosition = new Vector2(-size / 2f - 20f, leftIcon.anchoredPosition.y);
        rightIcon.anchoredPosition = new Vector2(size / 2f + 20f, rightIcon.anchoredPosition.y);

        for (int i = 0; i < (int)LanguageSelection.MaxIndex; i++)
        {
            if (i == (int)index)
            {
                languageChoiceText[i].color = new Color(0.4f, 0.0f, 0.0f);
            }
            else
            {
                languageChoiceText[i].color = Color.black;
            }
        }
    }

    public void SelectUpDown(float value)
    {
        if (menuState == MenuState.LANGUAGE_SELECT) LanguageSelectUpDown(value);
        if (menuState != MenuState.MAIN_MENU) return;

        if (value < 0)
        {
            SelectionUp();
        }
        else
        {
            SelectionDown();
        }
    }

    public void LanguageSelectUpDown(float value)
    {
        AudioManager.Instance.PlaySFX("cursor");
        if (value < 0)
        {
            languageSelectIndex--;
            if (languageSelectIndex < 0)
            {
                languageSelectIndex = LanguageSelection.MaxIndex - 1;
            }
        }
        else
        {
            languageSelectIndex++;
            if (languageSelectIndex >= LanguageSelection.MaxIndex)
            {
                languageSelectIndex = 0;
            }
        }
        ChangeLanguageSelection(languageSelectIndex);
    }

    public void SelectionUp()
    {
        if (disableMenuControl) return;

        AudioManager.Instance.PlaySFX("cursor");

        selectIndex--;
        if (selectIndex == MenuSelection.BossRushMode || selectIndex == MenuSelection.Exit || selectIndex == MenuSelection.Option)
        {
            selectIndex--;
        }
        if (selectIndex < 0)
        {
            selectIndex = MenuSelection.MaxIndex - 2;
        }
        ChangeSelection(selectIndex);
    }

    public void SelectionDown()
    {
        if (disableMenuControl) return;

        AudioManager.Instance.PlaySFX("cursor");

        selectIndex++;
        if (selectIndex == MenuSelection.BossRushMode || selectIndex == MenuSelection.Exit || selectIndex == MenuSelection.Option)
        {
            selectIndex++;
        }
        if (selectIndex >= MenuSelection.MaxIndex)
        {
            selectIndex = 0;
        }
        ChangeSelection(selectIndex);
    }

    public void SelectSelection()
    {
        if (menuState == MenuState.LANGUAGE_SELECT)
        {
            switch(languageSelectIndex)
            {
                case LanguageSelection.SimplifiedChinese:
                    LocalizationManagerHellFight.Instance().SetCurrentLanguage("ChineseSimplified");
                    break;
                case LanguageSelection.TraditionalChinese:
                    LocalizationManagerHellFight.Instance().SetCurrentLanguage("ChineseTraditional");
                    break;
                case LanguageSelection.Japanese:
                    LocalizationManagerHellFight.Instance().SetCurrentLanguage("Japanese");
                    break;
                case LanguageSelection.English:
                default:
                    LocalizationManagerHellFight.Instance().SetCurrentLanguage("English");
                    break;
            }
            ProtectedPlayerPrefs.SetString("Language", LocalizationManagerHellFight.Instance().GetCurrentLanguage());

            // transition to next state
            AudioManager.Instance.PlaySFX("startgame");
            StartCoroutine(StartMenuAnimation(languageSelectionParent.GetComponent<RectTransform>(), new Vector2(-553.0f, -0.0f), 0.9f));
        }
        if (menuState != MenuState.MAIN_MENU) return;
        if (disableMenuControl) return;
        disableMenuControl = true;
        switch (selectIndex)
        {
            case MenuSelection.MainGame:
                RecordPlayerName();
                AudioManager.Instance.StopMusicWithFade(0.1f);
                menuAlpha.DOFade(1.0f, 1.0f);
                StartCoroutine(startgame(1.1f));
                AudioManager.Instance.PlaySFX("Confirm");
                break;
            case MenuSelection.BossRushMode:
                disableMenuControl = false;
                SelectionUp();
                break;
            case MenuSelection.Leaderboard:
                StartCoroutine(OpenLeaderboardUI(0.4f));
                AudioManager.Instance.PlaySFX("decide");
                break;
            case MenuSelection.Option:
                disableMenuControl = false;
                SelectionUp();
                break;
            case MenuSelection.ResetLocalData:
                AudioManager.Instance.PlaySFX("decide");
                OpenResetDataMenu();
                break;
            case MenuSelection.Exit:
                disableMenuControl = false;
                SelectionUp();
                break;
            default:
                break;
        }
    }

    // show leaderboard
    IEnumerator OpenLeaderboardUI(float time)
    {
        // next page
        mainPageUI.gameObject.SetActive(false);
        pageAnimator.Play("BookFlipLeft");

        yield return new WaitForSecondsRealtime(time);

        leaderboardPageUI.gameObject.SetActive(true);

        ShowLeaderBoard(false);
        menuState = MenuState.LEADERBOARD;
    }

    private void RecordPlayerName()
    {
        // give a default name is there is no name written
        string nameWritten = playerNameText.text;

        if (nameWritten.Length == 0)
        {
            nameWritten = System.Environment.UserName;
            playerNameText.text = nameWritten;
        }

        gameMng.Initialize();
        gameMng.SetPlayerName(nameWritten);
    }

    IEnumerator startgame(float delay) 
    {
        yield return new WaitForSeconds(delay);
        gameMng.StartGame();
        menuAlpha.DOFade(0.0f, 0.0f);
    }

    public void HideLeaderBoard()
    {
        if (menuState != MenuState.LEADERBOARD) return;

        // avoid clicking too fast
        if (!leaderboardclickable) return;
        leaderboardObject.SetUnactiveLeaderboardButtonForSecond(0.5f);

        // PLAY SFX
        AudioManager.Instance.PlaySFX("closeLeaderboard");

        // DISABLE LEADERBOARD
        leaderboardObject.ResetList();
        leaderboardObject.GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f).SetUpdate(true);

        // RESET FLAG
        disableMenuControl = false;
        leaderboardObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
        menuState = MenuState.MAIN_MENU;
    }

    public void ChangeLeaderboard(int value)
    {
        // avoid clicking too fast
        if (!leaderboardclickable) return;
        leaderboardObject.SetUnactiveLeaderboardButtonForSecond(0.25f);

        int previousboard = (int)leaderboardType;
        previousboard += value;
        if (previousboard < 0) previousboard = (int)LeaderboardType.Max;
        if (previousboard > (int)LeaderboardType.Max) previousboard = (int)LeaderboardType.Min;

        // apply
        leaderboardType = (LeaderboardType)previousboard;

        // Refresh
        GetLeaderboardData(leaderboardType);

        // SE
        AudioManager.Instance.PlaySFX("page");
    }

    public LeaderboardType GetLeaderboardType()
    {
        return leaderboardType;
    }

    public void ShowLeaderBoard(bool refreshData)
    {
        menuState = MenuState.LEADERBOARD;
        
        // PLAY SFX
        AudioManager.Instance.PlaySFX("closeLeaderboard");

        // CHECK IF NEED TO REFRESH DATA
        if (refreshData)
        {
            for (int i = 0; i < 30; i++)
            {
                leaderboardRankList[i] = new Dictionary<string, string>();
            }
            GetLeaderboardData(leaderboardType);
        }
        else
        {

        }
    }

    private void GetLeaderboardData(LeaderboardType type)
    {
        for (int i = 0; i < 30; i++)
        {
            leaderboardRankList[i].Clear();
        }

        LootLockerSDKManager.GetScoreList(gameMng.GetLeaderboardID(leaderboardType), 30, (response) =>
        {
            if (response.success)
            {
                LootLocker.Requests.LootLockerLeaderboardMember[] scores = response.items;
                leaderBoardTotalEntry = 30;
                for (int i = 0; i < scores.Length; i++)
                {
                    // rank
                    leaderboardRankList[i]["data"] = scores[i].score.ToString();
                    //leaderboardRankList[i]["name"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "name");
                    //leaderboardRankList[i]["version"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "version");
                    //leaderboardRankList[i]["date"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "date");
                    //leaderboardRankList[i]["hp"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "hp");
                    //leaderboardRankList[i]["hpregen"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "hpregen");
                    //leaderboardRankList[i]["stamina"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "stamina");
                    //leaderboardRankList[i]["movespeed"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "movespeed");
                    //leaderboardRankList[i]["atkDmg"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "atkDmg");
                    //leaderboardRankList[i]["dashDmg"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "dashDmg");
                    //leaderboardRankList[i]["dashCD"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "dashCD");
                    //leaderboardRankList[i]["critical"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "critical");
                    //leaderboardRankList[i]["lifesteal"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "lifesteal");
                    //leaderboardRankList[i]["lifedrain"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "lifedrain");

                    // just name and level.
                    leaderboardRankList[i]["name"] = scores[i].member_id;

                    //Debug.Log("<color=blue>----------------------------------------------------------------</color>");
                    //Debug.Log("<color=red>" + scores[i].rank.ToString() + ". " + leaderboardRankList[i]["name"] + " </color>");
                    //Debug.Log("Level - " + leaderboardRankList[i]["level"]);
                    //Debug.Log("Version - " + leaderboardRankList[i]["version"]);
                    //Debug.Log("Date - " + leaderboardRankList[i]["date"]);
                    //Debug.Log("HP - " + leaderboardRankList[i]["hp"] + " + " + leaderboardRankList[i]["hpregen"] + "/s");
                    //Debug.Log("Stamina - " + leaderboardRankList[i]["stamina"]);
                    //Debug.Log("movespeed - " + leaderboardRankList[i]["movespeed"]);
                    //Debug.Log("atkDmg - " + leaderboardRankList[i]["atkDmg"]);
                    //Debug.Log("dashDmg - " + leaderboardRankList[i]["dashDmg"]);
                }
                if (scores.Length < 30)
                {
                    for (int i = scores.Length; i < 30; i++)
                    {
                        // rank
                        leaderboardRankList[i]["name"] = "???";
                        leaderBoardTotalEntry--;
                    }
                }
            }
            else
            {
                Debug.Log("Unable to connect server.");
            }
        });
    }

    private string LeaderboardValueSearchForKeyword(string memberID, string keyword)
    {
        string value = "???";

        if (memberID.Contains(keyword))
        {
            int starting = memberID.IndexOf(keyword) + keyword.Length;
            int ending = memberID.IndexOf("]", starting);

            value = memberID.Substring(starting+1, ending - starting -1);
        }

        return value;
    }

    public void SetLeaderboardButtonClickable(bool value)
    {
        leaderboardclickable = value;
    }

    public void ResetData()
    {
        // Delete Data
        PlayerPrefs.DeleteAll();

        // Close Menu
        CloseResetDataMenu();
        disableMenuControl = true;
        AudioManager.Instance.PlaySFX("cleardata", 0.5f);

        // close music
        AudioManager.Instance.StopMusicWithFade(1.0f);
        menuAlpha.DOFade(1.0f, 1.0f);

        // Back to start menu
        menuState = MenuState.START;
        StartCoroutine(SetActiveDelay(startGameText.gameObject, true, 1.1f));
        StartCoroutine(SetActiveDelay(logo.gameObject, false, 1.1f));
        StartCoroutine(SetActiveDelay(mainMenuUI.gameObject, false, 1.1f));

        StartCoroutine(RestartFromStartMenu(1.15f));
    }


    IEnumerator RestartFromStartMenu(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        menuAlpha.DOFade(0.0f, 1.0f);
    }

    public void OpenResetDataMenu()
    {
        AudioManager.Instance.PlaySFX("warning", 0.5f);
        resetDataPanel.gameObject.SetActive(true);
        resetDataPanel.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f);
        resetDataText.SetText("Name: <color=white>" + ProtectedPlayerPrefs.GetString("PlayerName") + "</color>\n" +
                              "Best Level: <color=white>" + ProgressManager.Instance().LoadUnlockLevel() + "</color>\n" +
                              "Last Death: <color=white>" + ProtectedPlayerPrefs.GetInt("LastDeath", 0) + "</color>\n");
    }

    public void CloseResetDataMenu()
    {
        AudioManager.Instance.PlaySFX("decide", 0.5f);
        resetDataPanel.GetComponent<CanvasGroup>().DOFade(0.0f, 0.5f);
        StartCoroutine(SetActiveDelay(resetDataPanel.gameObject, false, 0.5f));
        disableMenuControl = false;

    }

    IEnumerator SetActiveDelay(GameObject obj, bool boolean, float time)
    {
        yield return new WaitForSecondsRealtime(time);
        obj.SetActive(boolean);
    }

    private void SetupMainMenuUI()
    {
        highestRecordLevelText.text = Assets.SimpleLocalization.LocalizationManager.Localize("Menu.HighestRecordValue", ProgressManager.Instance().LoadUnlockLevel());
    }
}
