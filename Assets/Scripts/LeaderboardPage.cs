using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.SimpleLocalization;

public class LeaderboardPage : MonoBehaviour
{
    public enum LeaderboardMode
    {
        MainMenuMode,   // player is opening leaderboard from main menu.    
        GameOverMode,   // player is opening leaderboard from game over panel
    }

    [Header("References")]
    [SerializeField] Transform[] list;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text subtitle;
    [SerializeField] Menu menu;
    [SerializeField] Transform nextbutton;
    [SerializeField] Transform previousbutton;

    [Header("Speites")]
    [SerializeField] Sprite previousButtonSprite;
    [SerializeField] Sprite previousButtonSpritePressed;
    [SerializeField] Sprite homeButtonSprite;
    [SerializeField] Sprite homeButtonSpritePressed;

    LeaderboardType currentPage = LeaderboardType.Min;
    LeaderboardMode leaderboardMode = LeaderboardMode.MainMenuMode;

    public PlayerAction _input;

    private void Awake()
    {
        // INPUT SYSTEM
        _input = new PlayerAction();
        _input.MenuControls.LeaderboardMove.performed += ctx => InputLeftRight(ctx.ReadValue<float>());
    }

    private void OnEnable()
    {
        _input.Enable();

        UpdateButton();
    }
    private void OnDisable()
    {
        _input.Disable();
    }

    public void Setup(Dictionary<string, string>[] data, int totalData, int page)
    {
        currentPage = (LeaderboardType)page;
        for (int i = 0; i < list.Length; i++)
        {
            if (i < totalData)
            {
                
                list[i].Find("Index").GetComponent<TMP_Text>().text = (i+1).ToString() + ".";

                if (data[i].ContainsKey("name"))
                    list[i].Find("Name").GetComponent<TMP_Text>().text = data[i]["name"];

                if (data[i].ContainsKey("data"))
                    list[i].Find("Value").GetComponent<TMP_Text>().text = DataToStringConvertion(data[i]["data"], currentPage);
            }
            else
            {
                list[i].Find("Index").GetComponent<TMP_Text>().text = "";
                list[i].Find("Name").GetComponent<TMP_Text>().text = "";
                list[i].Find("Value").GetComponent<TMP_Text>().text = "";
            }
        }
    }

    public void PreviousPage()
    {
        if (currentPage == LeaderboardType.Min)
        {
            // back to main menu page
            switch (leaderboardMode)
            {
                case LeaderboardMode.MainMenuMode:
                    menu.HideLeaderBoard();
                    break;
                case LeaderboardMode.GameOverMode:
                    menu.gameObject.SetActive(false);
                    break;
                default:
                    break;
            }
        }
        else
        {
            currentPage--;
            menu.ChangeLeaderboard(currentPage);
            UpdateButton();
        }
    }

    public void NextPage()
    {
        if (currentPage < LeaderboardType.Max)
        {
            currentPage++;
            menu.ChangeLeaderboard(currentPage);
            UpdateButton();
        }
    }

    private void UpdateButton()
    {
        if (currentPage == LeaderboardType.Min)
        {
            previousbutton.GetComponent<Image>().sprite = homeButtonSprite;

            // change pressed sprite of button
            SpriteState spriteState = new SpriteState();
            spriteState = previousbutton.GetComponent<Selectable>().spriteState;
            spriteState.pressedSprite = homeButtonSpritePressed;
            previousbutton.GetComponent<Selectable>().spriteState = spriteState;
        }
        else
        {
            previousbutton.GetComponent<Image>().sprite = previousButtonSprite;

            // change pressed sprite of button
            SpriteState spriteState = new SpriteState();
            spriteState = previousbutton.GetComponent<Selectable>().spriteState;
            spriteState.pressedSprite = previousButtonSpritePressed;
            previousbutton.GetComponent<Selectable>().spriteState = spriteState;
        }

        nextbutton.gameObject.SetActive(currentPage != LeaderboardType.Max);
    }

    public void InputLeftRight(float value)
    {
        if (value < 0)
        {
            // left
            if (previousbutton.gameObject.activeInHierarchy)
            {
                // previousbutton.GetComponent<Button>().ClickTrigger();
                PreviousPage();
            }
        }
        else
        {
            // right
            if (nextbutton.gameObject.activeInHierarchy)
            {
                // nextbutton.GetComponent<Button>().ClickTrigger();
                NextPage();
            }
        }
    }

    private string DataToStringConvertion(string data, LeaderboardType type)
    {
        int dataInt;
        int.TryParse(data, out dataInt);

        string rtn = dataInt.ToString();
        switch (type)
        {
            case LeaderboardType.Level:
                rtn = "Level " + dataInt.ToString();
                break;
            case LeaderboardType.SpeedRunLevel10:
            case LeaderboardType.SpeedRunLevel20:
                rtn = (dataInt / 60).ToString() + "m" + (dataInt % 60).ToString() + "s";
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }

        return rtn;
    }

    public void SetLeaderboardType(LeaderboardType type)
    {
        currentPage = type;
        switch (type)
        {
            case LeaderboardType.Level:
                title.SetText(LocalizationManager.Localize("Leaderboard.BestLevel"));
                subtitle.SetText(LocalizationManager.Localize("Leaderboard.BestLevelDescription"));
                break;
            case LeaderboardType.SpeedRunLevel10:
                title.SetText(LocalizationManager.Localize("Leaderboard.Speedrun10"));
                subtitle.SetText(LocalizationManager.Localize("Leaderboard.Speedrun10Description"));
                break;
            case LeaderboardType.SpeedRunLevel20:
                title.SetText(LocalizationManager.Localize("Leaderboard.Speedrun20"));
                subtitle.SetText(LocalizationManager.Localize("Leaderboard.Speedrun20Description"));
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }
    }

    public void SetLeaderboardMode(LeaderboardMode mode)
    {
        leaderboardMode = mode;
    }
}
