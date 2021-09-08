using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using LootLocker.Requests;

public class Menu : MonoBehaviour
{
    enum MenuState
    {
        NONE,
        START,
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

    [SerializeField] Image frame;
    [SerializeField] Image background;
    [SerializeField] TMP_Text logo;
    [SerializeField] Image leaderboardBack;
    [SerializeField] TMP_Text versionText;
    [SerializeField] TMP_Text copyrightText;
    [SerializeField] Image menuAlpha;
    [SerializeField] GameManager gameMng;
    [SerializeField] TMP_InputField playerNameText;
    [SerializeField] Leaderboard leaderboardObject;
    [SerializeField] RectTransform resetDataPanel;
    [SerializeField] TMP_Text resetDataText;
    [SerializeField] TMP_Text startGameText;
    [SerializeField] RectTransform selectionParent;
    [SerializeField] RectTransform selectIcon;
    [SerializeField] TMP_Text[] choiceText;
    [SerializeField] RectTransform leftTransition;
    [SerializeField] RectTransform rightTransition;

    [Header("Debug")]
    [SerializeField] MenuSelection selectIndex;
    [SerializeField] MenuState menuState;

    bool disableMenuControl = false;
    bool leaderboardclickable = false;

    [SerializeField] Dictionary<string, string>[] leaderboardRankList;
    int leaderBoardTotalEntry;

    private void Start()
    {
        // UI
        selectIndex = MenuSelection.MainGame;
        disableMenuControl = true;
        leaderboardclickable = true;

        // LOCAL NAME SAVED
        playerNameText.text = PlayerPrefs.GetString("PlayerName", string.Empty); ;

        // UPDATE VERSION NAME
        versionText.SetText("version " + Application.version + " BETA");

        // CONNECT TO LEADERBOARD
        LootLockerSDKManager.StartSession("Player", (response) =>
        {
            if (response.success)
            {
                leaderboardRankList = new Dictionary<string, string>[30];
                for (int i = 0; i < 30; i++)
                {
                    leaderboardRankList[i]= new Dictionary<string, string>();
                }
                GetLeaderboardData();
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

    private void StartMenu()
    {
        menuState = MenuState.NONE;
        AudioManager.Instance.PlaySFX("startgame");
        StartCoroutine(StartMenuAnimation(startGameText.gameObject));
    }

    private void FinishNameInput()
    {
        if (playerNameText.text.Length <= 0) return;

        // DISABLE MENU INPUT
        menuState = MenuState.NONE;
        // DISABLE NAME INPUT
        playerNameText.DeactivateInputField();
        // SET TO GAME MANAGER
        gameMng.SetPlayerName(playerNameText.text);
        // SE
        AudioManager.Instance.PlaySFX("startgame");
        // ANIMATION
        StartCoroutine(StartMenuAnimation(playerNameText.gameObject));
    }

    IEnumerator StartMenuAnimation(GameObject gameobj)
    {
        for (int i = 0; i < 7; i++)
        {
            gameobj.SetActive(!gameobj.activeSelf);
            yield return new WaitForSeconds(0.18f);
        }

        // STATE
        playerNameText.text = PlayerPrefs.GetString("PlayerName", string.Empty);
        if (playerNameText.text == string.Empty)
        {
            menuState = MenuState.NAME_INPUT;
        }
        else
        {
            menuState = MenuState.MAIN_MENU;
        }

        switch (menuState)
        {
            case MenuState.MAIN_MENU:
                // ACTIVE UI COMPONENT
                logo.gameObject.SetActive(true);
                selectionParent.gameObject.SetActive(true);
                selectIndex = MenuSelection.MainGame;

                // SE
                AudioManager.Instance.PlaySFX("decide");
                AudioManager.Instance.PlayMusic("Battle-Sanctuary");

                // FLAG
                disableMenuControl = false;

                // Default selection choice
                ChangeSelection(MenuSelection.MainGame, 107.375f);
                break;
            case MenuState.NAME_INPUT:
                playerNameText.gameObject.SetActive(true);
                break;
        }
        
    }

    private void Update()
    {
        // INPUT FOR DIFFERENT STATE
        switch (menuState)
        {
            case MenuState.START:
                if (Input.anyKeyDown)
                {
                    StartMenu();
                }
                break;
            case MenuState.NAME_INPUT:
                if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Start"))
                {
                    FinishNameInput();
                }
                break;
            case MenuState.MAIN_MENU:
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || ((Input.anyKeyDown) && Input.GetAxisRaw("JoyPadVertical") == -1))
                {
                    SelectionDown();
                }
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || ((Input.anyKeyDown) && Input.GetAxisRaw("JoyPadVertical") == 1))
                {
                    SelectionUp();
                }
                if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J)
                     || Input.GetButtonDown("Dash"))
                {
                    SelectSelection();
                }
                break;
            case MenuState.LEADERBOARD:
                if (Input.GetKeyDown(KeyCode.Escape)
                     || Input.GetButtonDown("Spell"))
                {
                    HideLeaderBoard();
                }
                break;
        }
    }

    private void ChangeSelection(MenuSelection index, float offset = -1)
    {
        selectIcon.anchoredPosition = new Vector2(0f, choiceText[(int)index].GetComponent<RectTransform>().anchoredPosition.y);

        RectTransform leftIcon = selectIcon.GetChild(0).GetComponent<RectTransform>();
        RectTransform rightIcon = selectIcon.GetChild(1).GetComponent<RectTransform>();

        float size = offset == -1 ? choiceText[(int)index].textBounds.size.x : offset;

        leftIcon.anchoredPosition = new Vector2(-size / 2f - 20f, leftIcon.anchoredPosition.y);
        rightIcon.anchoredPosition = new Vector2(size / 2f + 20f, rightIcon.anchoredPosition.y);

        for (int i = 0; i < (int)MenuSelection.MaxIndex ;i++)
        {
            if (i == (int)index)
            {
                choiceText[i].color = Color.red;
            }
            else
            {
                choiceText[i].color = Color.white;
            }
        }
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
                StartCoroutine(MenuTransition(3f));
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

    IEnumerator MenuTransition(float time)
    {
        leftTransition.DOAnchorPosX(-30f, time / 2f);
        rightTransition.DOAnchorPosX(30f, time / 2f);

        yield return new WaitForSecondsRealtime(time / 2f);
        ShowLeaderBoard(false);
        menuState = MenuState.LEADERBOARD;

        leftTransition.DOAnchorPosX(-800f, time / 2f);
        rightTransition.DOAnchorPosX(800f, time / 2f);
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

    public void ShowLeaderBoard(bool refreshData)
    {
        // avoid clicking too fast
        if (!leaderboardclickable) return;

        // ENABLE LEADERBOARD
        leaderboardObject.gameObject.SetActive(true);
        leaderboardObject.SetUnactiveLeaderboardButtonForSecond(0.5f);

        // PLAY SFX
        AudioManager.Instance.PlaySFX("closeLeaderboard");

        // CHECK IF NEED TO REFRESH DATA
        if (refreshData)
        {
            for (int i = 0; i < 30; i++)
            {
                leaderboardRankList[i] = new Dictionary<string, string>();
            }
            GetLeaderboardData(true);
        }
        else
        {
            // INITIATE
            leaderboardObject.Initialize(leaderboardRankList, leaderBoardTotalEntry);
        }

        // LEADERBOARD UI
        leaderboardObject.GetComponent<CanvasGroup>().DOFade(0.0f, 0.0f).SetUpdate(true);
        leaderboardObject.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f).SetUpdate(true);
        leaderboardObject.GetComponent<CanvasGroup>().blocksRaycasts = true;
        leaderboardObject.SetValueScrollBar(0.1f, 1.0f);
    }

    private void GetLeaderboardData(bool refreshData = false)
    {
        for (int i = 0; i < 30; i++)
        {
            leaderboardRankList[i].Clear();
        }

        LootLockerSDKManager.GetScoreList(gameMng.GetLeaderboardID(), 30, (response) =>
        {
            if (response.success)
            {
                LootLocker.Requests.LootLockerLeaderboardMember[] scores = response.items;
                leaderBoardTotalEntry = 30;
                for (int i = 0; i < scores.Length; i++)
                {
                    // rank
                    leaderboardRankList[i]["level"] = scores[i].score.ToString();
                    leaderboardRankList[i]["name"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "name");
                    leaderboardRankList[i]["version"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "version");
                    leaderboardRankList[i]["date"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "date");
                    leaderboardRankList[i]["hp"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "hp");
                    leaderboardRankList[i]["hpregen"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "hpregen");
                    leaderboardRankList[i]["stamina"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "stamina");
                    leaderboardRankList[i]["movespeed"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "movespeed");
                    leaderboardRankList[i]["atkDmg"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "atkDmg");
                    leaderboardRankList[i]["dashDmg"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "dashDmg");
                    leaderboardRankList[i]["dashCD"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "dashCD");
                    leaderboardRankList[i]["critical"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "critical");
                    leaderboardRankList[i]["lifesteal"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "lifesteal");
                    leaderboardRankList[i]["lifedrain"] = LeaderboardValueSearchForKeyword(scores[i].member_id, "lifedrain");

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

                if (refreshData)
                {
                    // UPDATE
                    leaderboardObject.Refresh(leaderboardRankList, leaderBoardTotalEntry);
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
        StartCoroutine(SetActiveDelay(selectionParent.gameObject, false, 1.1f));

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
        resetDataText.SetText("Name: <color=white>" + PlayerPrefs.GetString("PlayerName") + "</color>\n" +
                              "Best Level: <color=white>" + ProgressManager.Instance().LoadUnlockLevel() + "</color>\n" +
                              "Last Death: <color=white>" + PlayerPrefs.GetInt("LastDeath", 0) + "</color>\n");
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
}
