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
    // ���j���[�V�[���̗���
    enum MenuState
    {
        NONE,
        START,
        LANGUAGE_SELECT,
        NAME_INPUT,
        MAIN_MENU,
        LEADERBOARD,
        CREDITS,
    }

    // �^�C�g�����j���[�̑I����
    enum MenuSelection
    {
        MainGame,
        BossRushMode,
        Leaderboard,
        Credits,
        ResetLocalData,
        Exit,

        MaxIndex
    }

    // ����I��
    enum LanguageSelection
    {
        English,
        SimplifiedChinese,
        TraditionalChinese,
        Japanese,

        MaxIndex
    }

    [SerializeField] Animator pageAnimator;
    [SerializeField] Image background;
    [SerializeField] TMP_Text logo;
    [SerializeField] TMP_Text versionText;
    [SerializeField] GameObject copyrightUI;
    [SerializeField] GameManager gameMng;
    [SerializeField] TMP_InputField playerNameText;
    [SerializeField] RectTransform playerNameText_UI;
    [SerializeField] RectTransform resetDataComfirmation;
    [SerializeField] GameObject startGameText;
    [SerializeField] RectTransform mainMenuUI;
    [SerializeField] RectTransform mainPageUI;
    [SerializeField] RectTransform leaderboardPageUI;
    [SerializeField] RectTransform creditsPageUI;
    [SerializeField] RectTransform languageSelectionParent;
    [SerializeField] RectTransform selectIcon;
    [SerializeField] RectTransform languageSelectIcon;
    [SerializeField] TMP_Text[] choiceText;
    [SerializeField] TMP_Text[] languageChoiceText;
    [SerializeField] TMP_Text highestRecordLevelText;
    [SerializeField] TMP_Text unlockLevelText;

    [Header("Debug")]
    [SerializeField] MenuSelection selectIndex;
    [SerializeField] LanguageSelection languageSelectIndex;
    [SerializeField] MenuState menuState;
    [SerializeField] LeaderboardType leaderboardType;

    // �V�[���J�ځE�Ǘ�
    List<MenuState> menuQueue = new List<MenuState>();

    // ���[�_�[�{�[�h
    [SerializeField] Dictionary<string, string>[] leaderboardRankList;
    int leaderBoardTotalEntry;
    bool leaderboardDataLoading = false;

    // ���͊֘A
    public PlayerAction input;
    bool disableMenuControl = false;

    private void Start()
    {
        // UI
        selectIndex = MenuSelection.MainGame;
        disableMenuControl = true;
        leaderboardDataLoading = true;

        // �Z�[�u�f�[�^��������
        // INITIALIZE GAME SAVE FILE (https://github.com/richardelms/FileBasedPlayerPrefs)
        // configuration
        var config = new FBPPConfig()
        {
            SaveFileName = "FBPP.txt",
            AutoSaveData = false,
            ScrambleSaveData = true,
            EncryptionSecret = "encryption-secret-default",
            SaveFilePath = Application.persistentDataPath
        };

        Debug.Log("���[�J���Z�[�u�f�[�^�ʒu�F" + Application.persistentDataPath);
        FBPP.Start(config);

        // �Z�[�u�f�[�^�𑶍݂��Ă���Ȃ烍�[�h����
        // LOAD SAVE DATA
        ProgressManager.Instance().Initialization();
        ProgressManager.Instance().LoadProgress();

        // ���[�J���C�Y�V�X�e����������
        // INITIALIZE LOCALIZATION
        string language = FBPP.GetString("Language", string.Empty);
        LocalizationManagerHellFight.Instance().Initialization(Application.systemLanguage);
        LocalizationManagerHellFight.Instance().SetCurrentLanguage(language);

        // �v���C���[�������[�h
        // LOCAL NAME SAVED
        playerNameText.text = FBPP.GetString("PlayerName", string.Empty); ;

        // �o�[�W��������ݒ�
        // UPDATE VERSION NAME
        versionText.SetText("ver. " + Application.version + "(beta)");

        // ���[�_�[�{�[�h�ɐڑ�
        // CONNECT TO LEADERBOARD
        LootLockerSDKManager.StartAndroidSession("Player", (response) =>
        {
            Debug.Log("���[�_�[�{�[�h�ڑ��J�n...");
            if (response.success)
            {
                Debug.Log("���[�_�[�{�[�h�ڑ�����");
                leaderboardRankList = new Dictionary<string, string>[20];
                for (int i = 0; i < 20; i++)
                {
                    leaderboardRankList[i] = new Dictionary<string, string>();
                }
                leaderboardType = LeaderboardType.Level;
                //if (ProgressManager.Instance().LoadUnlockLevel() > 10 ) leaderboardType = LeaderboardType.SpeedRunLevel10;
                //if (ProgressManager.Instance().LoadUnlockLevel() > 20 ) leaderboardType = LeaderboardType.SpeedRunLevel20;
                GetLeaderboardData(leaderboardType, false);
            }
        });

        // �I�[�f�B�I�V�X�e����������
        // MUSIC MANAGER
        AudioManager.Instance.SetMusicVolume(0.7f);
        AudioManager.Instance.SetSEMasterVolume(0.25f);

        // INIT
        menuQueue.Clear();
        menuQueue.Add(MenuState.START);
        menuQueue.Add(MenuState.LANGUAGE_SELECT);
        menuQueue.Add(MenuState.NAME_INPUT);
        menuQueue.Add(MenuState.MAIN_MENU);
        NextMenu();
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
        input = new PlayerAction();
        input.MenuControls.Move.performed += ctx => SelectUpDown(ctx.ReadValue<float>());
        input.MenuControls.AnyKey.performed += ctx =>   StartMenu();
        input.MenuControls.Confirm.performed += ctx =>  FinishNameInput();
        input.MenuControls.Confirm.performed += ctx =>  SelectSelection();
        input.MenuControls.Cancel.performed += ctx =>   HideLeaderBoard();
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void StartMenu()
    {
        if (menuState != MenuState.START) return;

        menuState = MenuState.NONE;
        AudioManager.Instance.PlaySFX("startgame");

        const float animTime = 0.5f;
        StartCoroutine(StartMenuAnimation(startGameText, new Vector2(-391.0f, -265.0f), animTime));
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
        StartCoroutine(StartMenuAnimation(playerNameText_UI.gameObject, new Vector2(-391.0f, -265.0f), 1.5f));
    }

    /// <summary>
    /// ���j���[��̃A�j���[�V�����Đ�
    /// </summary>
    IEnumerator StartMenuAnimation(GameObject obj, Vector2 endPos, float animTime)
    {
        // play animation as requested
        Vector3 originalPos = obj.transform.position;
        Color originalColor = Color.white;
        if (!ReferenceEquals(obj.GetComponent<Image>(), null))
        {
            originalColor = obj.GetComponent<Image>().color;
            obj.GetComponent<Image>().DOFade(0.0f, animTime);
        }
        else if (!ReferenceEquals(obj.GetComponent<CanvasGroup>(), null))
        {
            obj.GetComponent<CanvasGroup>().DOFade(0.0f, animTime);
        }

        yield return new WaitForSeconds(animTime);

        // return to original position and hide it.
        obj.gameObject.SetActive(false);
        if (!ReferenceEquals(obj.GetComponent<Image>(), null))
        {
            obj.GetComponent<Image>().color = originalColor;
        }
        else if (!ReferenceEquals(obj.GetComponent<CanvasGroup>(), null))
        {
            obj.GetComponent<CanvasGroup>().DOFade(1.0f, animTime);
        }
        obj.transform.position = originalPos;

        // ���̃��j���[�V�[���֑J��
        NextMenu();
    }

    /// <summary>
    /// ���̃��j���[��\��
    /// </summary>
    private void NextMenu()
    {
        // ���̃��j���[��\��
        if (menuQueue.Count > 0)
        {
            menuState = menuQueue[0];
            menuQueue.Remove(menuState);
        }
        else
        {
            // �Ƃ肠�������C�����j���[��\��
            menuState = MenuState.MAIN_MENU;
        }

        switch (menuState)
        {
            case MenuState.START:
            {
                 break;
            }
            case MenuState.LANGUAGE_SELECT:
            {
                if (GetCurrentLanguageSetting() != string.Empty)
                {
                    // ���łɐݒ肳��Ă���
                    NextMenu();
                    break;
                }

                languageSelectionParent.gameObject.SetActive(true);
                languageSelectIndex = GetDefaultLanguageSelection();

                // Default selection choice
                ChangeLanguageSelection(languageSelectIndex, 160f);
                break;
            }
            case MenuState.NAME_INPUT:
            {
                if (playerNameText.text != string.Empty)
                {
                    // ���O�͂��łɐݒ肳��Ă���
                    NextMenu();
                    break;
                }

                playerNameText.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
                playerNameText_UI.gameObject.SetActive(true);
                playerNameText.interactable = true;
                playerNameText.Select();
                break;
            }
            case MenuState.MAIN_MENU:
            {
                StartCoroutine(StartMainMenu());
                break;
            }
            default:
            {
                Debug.Log("���̃V�[�����Ȃ�");
            }
                break;
        }
    }

    /// <summary>
    /// ���݂̃Q�[������ݒ���擾����
    /// �ݒ肳��Ă��Ȃ���΋�(Empty)��Ԃ�
    /// </summary>
    private string GetCurrentLanguageSetting()
    {
        string language = string.Empty;

        // STEAM
        if (SteamworksNetManager.Instance().IsSteamConnected())
        {
            playerNameText.text = SteamworksNetManager.Instance().GetSteamID();
            language = SteamworksNetManager.Instance().GetSteamLanguage(true);
            LocalizationManagerHellFight.Instance().SetCurrentLanguage(language);
            FBPP.SetString("Language", language); // save setting into disk
            FBPP.Save();
        }
        else // other platform
        {
            playerNameText.text = FBPP.GetString("PlayerName", string.Empty);
            language = FBPP.GetString("Language", string.Empty);
        }

        return language;
    }

    /// <summary>
    /// �f�t�H���g�̃V�X�e��������擾
    /// </summary>
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

    /// <summary>
    /// ���C�����j���[��������
    /// </summary>
    private IEnumerator StartMainMenu()
    {
        // ACTIVE UI COMPONENT
        logo.gameObject.SetActive(true);
        mainMenuUI.gameObject.SetActive(true);
        selectIndex = MenuSelection.MainGame;

        // SE
        AudioManager.Instance.PlaySFX("decide");
        AudioManager.Instance.PlayMusicWithFade("The March", 2.0f);

        // Setup Text
        SetupMainMenuUI();
        // Default selection choice
        ChangeSelection(MenuSelection.MainGame, 107.375f);

        // �A�j���[�V�����Đ�
        // Animation
        float mainMenuAnimTime = 2.0f;
        AudioManager.Instance.PlaySFX("stamp", 0.7f);
        mainMenuUI.GetComponent<CanvasGroup>().alpha = 0.0f;
        mainMenuUI.GetComponent<CanvasGroup>().DOFade(1.0f, mainMenuAnimTime);
        mainMenuUI.localScale = new Vector3(3.0f, 3.0f, 1.0f);

        mainMenuUI.DOScale(1.0f, mainMenuAnimTime * 0.25f);
        yield return new WaitForSeconds(mainMenuAnimTime * 0.25f);
        mainMenuUI.DOShakePosition(0.5f, 9, 20, 90);

        // �w�i�̐F��ݒ�
        Color backgroundColor = background.color;
        background.color = new Color(0.3f, 0.1f, 0.1f, 1.0f);
        background.DOColor(backgroundColor, 0.75f);

        // FLAG
        disableMenuControl = false;
    }

    /// <summary>
    /// ���C�����j���[�̑I�𐧌�
    /// </summary>
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

    /// <summary>
    /// ����I�𐧌�
    /// </summary>
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
        if (selectIndex == MenuSelection.BossRushMode
#if DISABLESTEAMWORKS
            || selectIndex == MenuSelection.Exit 
#endif
            )
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
        if (selectIndex == MenuSelection.BossRushMode
#if DISABLESTEAMWORKS
            || selectIndex == MenuSelection.Exit
#endif
            )
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
            FBPP.SetString("Language", LocalizationManagerHellFight.Instance().GetCurrentLanguage());
            FBPP.Save();

            // transition to next state
            AudioManager.Instance.PlaySFX("startgame");
            StartCoroutine(StartMenuAnimation(languageSelectionParent.gameObject, new Vector2(-553.0f, -0.0f), 0.9f));
        }
        if (menuState != MenuState.MAIN_MENU) return;
        if (disableMenuControl) return;
        disableMenuControl = true;
        switch (selectIndex)
        {
            case MenuSelection.MainGame:
                RecordPlayerName();
                AudioManager.Instance.StopMusicWithFade(0.1f);
                StartCoroutine(startgame(1.1f));
                AudioManager.Instance.PlaySFX("Confirm");
                break;
            case MenuSelection.BossRushMode:
                disableMenuControl = false;
                SelectionUp();
                break;
            case MenuSelection.Leaderboard:
                leaderboardType = LeaderboardType.Min; // always open the first page.
                StartCoroutine(OpenLeaderboardUI(0.4f, false));
                leaderboardPageUI.GetComponent<LeaderboardPage>().SetLeaderboardMode(LeaderboardPage.LeaderboardMode.MainMenuMode);
                AudioManager.Instance.PlaySFX("confirmMenu");
                break;
            case MenuSelection.Credits:
                disableMenuControl = false;
                StartCoroutine(CreditNextPage(0.4f, "BookFlipLeft"));
                SelectionUp();
                break;
            case MenuSelection.ResetLocalData:
                AudioManager.Instance.PlaySFX("confirmMenu");
                OpenResetDataMenu();
                break;
            case MenuSelection.Exit:
                disableMenuControl = false;
                Application.Quit();
                break;
            default:
                break;
        }
    }

    // show/hide leaderboard
    IEnumerator OpenLeaderboardUI(float time, bool refreshData = false)
    {
        // next page
        mainPageUI.gameObject.SetActive(false);
        pageAnimator.Play("BookFlipLeft");
        // SE
        AudioManager.Instance.PlaySFX("page");

        yield return new WaitForSecondsRealtime(time);

        leaderboardPageUI.gameObject.SetActive(true);

        ShowLeaderBoard(refreshData);
        menuState = MenuState.LEADERBOARD;
    }
    IEnumerator HideLeaderboardUI(float time)
    {
        // next page
        leaderboardPageUI.gameObject.SetActive(false);
        pageAnimator.Play("BookFlipRight");
        // SE
        AudioManager.Instance.PlaySFX("page");

        yield return new WaitForSecondsRealtime(time);

        mainPageUI.gameObject.SetActive(true);
        disableMenuControl = false;

        menuState = MenuState.MAIN_MENU;
    }
    public void OpenLeaderboardFromGameOverPanel()
    {
        transform.gameObject.SetActive(true);

        leaderboardType = LeaderboardType.Min; // always open the first page.
        StartCoroutine(OpenLeaderboardUI(0.4f, true));
        leaderboardPageUI.GetComponent<LeaderboardPage>().SetLeaderboardMode(LeaderboardPage.LeaderboardMode.GameOverMode);
    }

    IEnumerator LeaderboardNextPage(float time, string animationName)
    {
        // next page
        leaderboardPageUI.gameObject.SetActive(false);

        while (leaderboardDataLoading)
        {
            // SE
            AudioManager.Instance.PlaySFX("page");
            // play animation until data loaded complete
            pageAnimator.Play(animationName);
            yield return new WaitForSecondsRealtime(time);
        }

        leaderboardPageUI.gameObject.SetActive(true);

        ShowLeaderBoard(true);
        disableMenuControl = true;
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
        // animations
        mainMenuUI.DOScale(0.8f, delay);
        mainMenuUI.GetComponent<CanvasGroup>().DOFade(0.0f, delay);
        Color originalColor = background.color;
        background.DOColor(Color.black, delay);

        yield return new WaitForSeconds(delay);
        gameMng.StartGame();

        // reset everything to original
        background.color = originalColor;
        mainMenuUI.localScale = new Vector3(1, 1, 1);
        mainMenuUI.GetComponent<CanvasGroup>().alpha = 1.0f;
    }

    public void HideLeaderBoard()
    {
        if (menuState != MenuState.LEADERBOARD) return;

        StartCoroutine(HideLeaderboardUI(0.4f));
    }

    public void ChangeLeaderboard(LeaderboardType value)
    {
        LeaderboardType previouspage = leaderboardType;
        // apply
        leaderboardType = value;

        // Refresh
        GetLeaderboardData(leaderboardType, true);

        // Animation
        string animName = previouspage < leaderboardType ? "BookFlipLeft" : "BookFlipRight";
        StartCoroutine(LeaderboardNextPage(0.4f, animName));
    }

    /// <summary>
    /// ���\������Ă��郊�[�_�[�{�[�h���擾
    /// </summary>
    public LeaderboardType GetLeaderboardType()
    {
        return leaderboardType;
    }

    /// <summary>
    /// ���[�_�[�{�[�h��\������
    /// </summary>
    public void ShowLeaderBoard(bool refreshData)
    {
        menuState = MenuState.LEADERBOARD;

        // CHECK IF NEED TO REFRESH DATA
        if (refreshData)
        {
            for (int i = 0; i < 20; i++)
            {
                leaderboardRankList[i] = new Dictionary<string, string>();
            }
            GetLeaderboardData(leaderboardType, refreshData);
        }

        // since the data doesn't need to be updated.
        // instead of wait until the data transfered, we should setup the list ui immediately.
        leaderboardPageUI.GetComponent<LeaderboardPage>().Setup(leaderboardRankList, leaderBoardTotalEntry, (int)leaderboardType);
        leaderboardPageUI.GetComponent<LeaderboardPage>().SetLeaderboardType(leaderboardType);
    }

    /// <summary>
    /// ���[�_�[�{�[�h�̃f�[�^���T�[�o�[����擾
    /// </summary>
    private void GetLeaderboardData(LeaderboardType type, bool setupUIAfterSuccess)
    {
        if (!ReferenceEquals(leaderboardRankList, null))
        {
            for (int i = 0; i < 20; i++)
            {
                leaderboardRankList[i].Clear();
            }
        }

        leaderboardDataLoading = true;

        LootLockerSDKManager.GetScoreList(gameMng.GetLeaderboardKey(leaderboardType), 20, (response) =>
        {
            if (response.success)
            {
                LootLocker.Requests.LootLockerLeaderboardMember[] scores = response.items;
                leaderBoardTotalEntry = 20;
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
                if (scores.Length < 20)
                {
                    for (int i = scores.Length; i < 20; i++)
                    {
                        // rank
                        leaderboardRankList[i]["name"] = "???";
                        leaderBoardTotalEntry--;
                    }
                }

                if (setupUIAfterSuccess)
                {
                    leaderboardPageUI.GetComponent<LeaderboardPage>().Setup(leaderboardRankList, leaderBoardTotalEntry, (int)leaderboardType);
                }
                leaderboardDataLoading = false;
            }
            else
            {
                leaderboardDataLoading = false;
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

    public bool IsLeaderboardDataLoading()
    {
        return leaderboardDataLoading;
    }

    public void ResetData()
    {
        // Delete Data
        FBPP.DeleteAll();
        ProgressManager.Instance().ResetProgress();

        // Close Menu
        CloseResetDataMenu();
        disableMenuControl = true;
        AudioManager.Instance.PlaySFX("cleardata", 0.5f);

        // close music
        AudioManager.Instance.StopMusicWithFade(1.0f);

        // Back to start menu
        StartCoroutine(SetActiveDelay(startGameText.gameObject, true, 1.1f));
        StartCoroutine(SetActiveDelay(logo.gameObject, false, 1.1f));
        StartCoroutine(SetActiveDelay(mainMenuUI.gameObject, false, 1.1f));

        // �V�[���J�ڂ�������
        // INIT
        menuQueue.Clear();
        menuQueue.Add(MenuState.START);
        menuQueue.Add(MenuState.LANGUAGE_SELECT);
        menuQueue.Add(MenuState.NAME_INPUT);
        menuQueue.Add(MenuState.MAIN_MENU);

        NextMenu();
    }

    public void OpenResetDataMenu()
    {
        AudioManager.Instance.PlaySFX("warning", 0.5f);
        resetDataComfirmation.gameObject.SetActive(true);
        resetDataComfirmation.GetComponent<CanvasGroup>().DOFade(1.0f, 0.1f);
    }

    public void CloseResetDataMenu()
    {
        AudioManager.Instance.PlaySFX("decide", 0.5f);
        resetDataComfirmation.GetComponent<CanvasGroup>().DOFade(0.0f, 0.1f);
        StartCoroutine(SetActiveDelay(resetDataComfirmation.gameObject, false, 0.1f));
        disableMenuControl = false;
    }

    IEnumerator SetActiveDelay(GameObject obj, bool boolean, float time)
    {
        yield return new WaitForSecondsRealtime(time);
        obj.SetActive(boolean);
    }

    private void SetupMainMenuUI()
    {
        highestRecordLevelText.text = Assets.SimpleLocalization.LocalizationManager.Localize("Menu.HighestRecordValue", 
            ProgressManager.Instance().GetHighestLevelRecord());

        unlockLevelText.text =
            ProgressManager.Instance().GetUnlockLevel() + "/" +ProgressManager.Instance().GetHighestUnlockLevel();
    }


    // ---------------------- CREDITS
    IEnumerator CreditNextPage(float time, string animationName)
    {
        // next page
        disableMenuControl = true;
        mainPageUI.gameObject.SetActive(false);

        // SE
        AudioManager.Instance.PlaySFX("page");
        pageAnimator.Play(animationName);
        yield return new WaitForSecondsRealtime(time);

        creditsPageUI.gameObject.SetActive(true);
        menuState = MenuState.CREDITS;
    }

    public void CreditsBackToMainMenu()
    {
        StartCoroutine(CreditBack(0.4f, "BookFlipRight"));
    }

    IEnumerator CreditBack(float time, string animationName)
    {
        // last page
        creditsPageUI.gameObject.SetActive(false);
        disableMenuControl = true;

        // SE
        AudioManager.Instance.PlaySFX("page");
        pageAnimator.Play(animationName);
        yield return new WaitForSecondsRealtime(time);

        disableMenuControl = false;
        mainPageUI.gameObject.SetActive(true);
        menuState = MenuState.MAIN_MENU;
    }
}
