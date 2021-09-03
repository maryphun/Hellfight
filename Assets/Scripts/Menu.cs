using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using LootLocker.Requests;

public class Menu : MonoBehaviour
{
    enum MenuSelection
    {
        StartGame,
        Leaderboard,
        Option,
        Credit,
        EndGame,

        MaxIndex
    }

    [SerializeField] Image frame;
    [SerializeField] Image background;
    [SerializeField] TMP_Text logo;
    [SerializeField] Image leaderboardBack;
    [SerializeField] Image selectionback;
    [SerializeField] RectTransform playerUI;
    [SerializeField] TMP_Text selectionText;
    [SerializeField] TMP_Text versionText;
    [SerializeField] TMP_Text copyrightText;
    [SerializeField] Animator leftArrow;
    [SerializeField] Animator rightArrow;
    [SerializeField] Image menuAlpha;
    [SerializeField] GameManager gameMng;
    [SerializeField] TMP_InputField playerNameText;
    [SerializeField] Leaderboard leaderboardObject;
    [SerializeField] TMP_Text startGameText;

    MenuSelection selectIndex;

    bool disableMenuControl = false;
    bool leaderboardclickable = false;
    bool menuStarted = false;

    [SerializeField] Dictionary<string, string>[] leaderboardRankList;
    int leaderBoardTotalEntry;

    private void Start()
    {
        // UI
        selectIndex = MenuSelection.StartGame;
        UpdateSelectionText(selectIndex);
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
    }

    private void StartMenu()
    {
        AudioManager.Instance.PlaySFX("startgame");
        StartCoroutine(StartMenuAnimation());
    }

    IEnumerator StartMenuAnimation()
    {
        startGameText.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.2f);
        startGameText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        startGameText.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.2f);
        startGameText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        startGameText.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.2f);

        // ACTIVE UI COMPONENT
        logo.gameObject.SetActive(true);
        logo.rectTransform.DOMoveY(1.5f, 2f);
        selectionText.gameObject.SetActive(true);
        selectionback.gameObject.SetActive(true);
        leftArrow.gameObject.SetActive(true);
        rightArrow.gameObject.SetActive(true);
        playerUI.gameObject.SetActive(true);
        playerNameText.gameObject.SetActive(true);

        // SE
        AudioManager.Instance.PlaySFX("decide");
        AudioManager.Instance.PlayMusic("Battle-Sanctuary");

        // FLAG
        disableMenuControl = false;
        menuStarted = true;
    }

    private void Update()
    {
        if (!menuStarted)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                StartMenu();
            }
            return;
        }

        // INPUT
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectionLeft();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectionRight();
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J))
        {
            SelectSelection();
        }
    }

    public void SelectionLeft()
    {
        if (disableMenuControl) return;
        AudioManager.Instance.PlaySFX("cursor");
        selectIndex--;
        if (selectIndex < 0)
        {
            selectIndex = MenuSelection.MaxIndex - 1;
        }
        UpdateSelectionText(selectIndex);
        leftArrow.Play("LeftArrowSelected");
        rightArrow.Play("rightArrowAnim", 0, 0f);
        ChangeThemeColor();
        playerUI.DOScaleX(-0.5f,0.0f);
    }
    public void SelectionRight()
    {
        if (disableMenuControl) return;
        AudioManager.Instance.PlaySFX("cursor");
        selectIndex++;
        if (selectIndex >= MenuSelection.MaxIndex)
        {
            selectIndex = 0;
        }
        UpdateSelectionText(selectIndex);
        leftArrow.Play("arrowAnim", 0, 0f);
        rightArrow.Play("RightArrowSelected");
        ChangeThemeColor();
        playerUI.DOScaleX(0.5f,0.0f);
    }

    public void SelectSelection()
    {
        if (disableMenuControl) return;
        AudioManager.Instance.PlaySFX("decide");
        playerUI.GetComponent<Animator>().Play("playerUIAttack");
        disableMenuControl = true;
        switch (selectIndex)
        {
            case MenuSelection.StartGame:
                RecordPlayerName();
                AudioManager.Instance.StopMusicWithFade(0.1f);
                menuAlpha.DOFade(1.0f, 1.0f);
                StartCoroutine(startgame(1.1f));
                break;
            case MenuSelection.Leaderboard:
                ShowLeaderBoard(false);
                break;
            case MenuSelection.Option:
                disableMenuControl = false;

                break;
            case MenuSelection.Credit:
                disableMenuControl = false;

                break;
            case MenuSelection.EndGame:
                disableMenuControl = false;

                break;
            default:
                break;
        }
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

    private void UpdateSelectionText(MenuSelection index)
    {
        switch (index)
        {
            case MenuSelection.StartGame:
                selectionText.SetText("Start Game");
                break;
            case MenuSelection.Leaderboard:
                selectionText.SetText("Leaderboard");
                break;
            case MenuSelection.Option:
                selectionText.SetText("Option");
                break;
            case MenuSelection.Credit:
                selectionText.SetText("Credit");
                break;
            case MenuSelection.EndGame:
                selectionText.SetText("Close Application");
                break;
            default:
                UpdateSelectionText(0);
                break;
        }
    }

    public void ChangeThemeColor()
    {
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

        leftArrow.GetComponent<Image>().color = newColor;
        rightArrow.GetComponent<Image>().color = newColor;
        frame.color = newColor;
        background.color = new Color(newColor.r / 10f, newColor.g / 10f, newColor.b/10f, 1f);
        selectionText.color = newColor;
        versionText.color = newColor;
        copyrightText.color = newColor;
        logo.color = newColor;
        selectionback.color = new Color(newColor.r, newColor.g, newColor.b, 0.2f);
        leaderboardBack.color = new Color(newColor.r, newColor.g, newColor.b, 1f);
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

        LootLockerSDKManager.GetScoreList(399, 30, (response) =>
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
}
