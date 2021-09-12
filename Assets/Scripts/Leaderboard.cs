using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] GameObject rankListPrefab;
    [SerializeField] TMP_Text rankType;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text description;
    [SerializeField] RectTransform listParent;
    [SerializeField] RectTransform head;
    [SerializeField] RectTransform detailCheckerPanel;
    [SerializeField] Image detailCheckerAlpha;
    [SerializeField] TMP_Text detailCheckerTitle;
    [SerializeField] TMP_Text detailCheckertext1;
    [SerializeField] TMP_Text detailCheckertext2;
    [SerializeField] TMP_Text versionText;
    [SerializeField] Scrollbar leaderboardScrollValue;
    [SerializeField] Menu menu;

    bool initialized = false;
    GameObject[] ranklist;

    // Update is called once per frame
    public void Initialize(Dictionary<string, string>[] data, int total)
    {
        listParent.sizeDelta = new Vector2(0, total * 40f + 70);
        head.anchoredPosition = new Vector3(0f, listParent.sizeDelta.y / 2f - head.sizeDelta.y /2f + 22.5f, 0f);
       
        ranklist = new GameObject[total];
        for (int i = 0; i < total; i ++)
        {
            GameObject tmp = Instantiate(rankListPrefab, new Vector3(0,0,0), Quaternion.identity);
            tmp.transform.SetParent(listParent.transform, true);
            tmp.GetComponent<RectTransform>().anchoredPosition = new Vector3(0f,  listParent.sizeDelta.y /2f - 70f + (-40f * i), 0f);
            tmp.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
           
            {
                tmp.transform.GetChild(0).GetComponent<TMP_Text>().SetText((i + 1).ToString() + ".");
                tmp.transform.GetChild(1).GetComponent<TMP_Text>().SetText(data[i]["name"]);
                tmp.transform.GetChild(2).GetComponent<DetailChecker>().InsertDetail(data[i]);
                //tmp.transform.GetChild(3).GetComponent<TMP_Text>().SetText(data[i]["date"]);
                int dataInt;
                int.TryParse(data[i]["data"], out dataInt);
                tmp.transform.GetChild(4).GetComponent<TMP_Text>().SetText(DataToStringConvertion(dataInt, menu.GetLeaderboardType()));
            }

            ranklist[i] = tmp;
        }
        initialized = true;
    }

    public void SetLeaderboardType(LeaderboardType type)
    {
        switch (type)
        {
            case LeaderboardType.Level:
                rankType.SetText("LEVEL");
                title.SetText("BEST LEVEL");
                description.SetText("Player that explored the deepest of the dungeon");
                break;
            case LeaderboardType.SpeedRunLevel10:
                rankType.SetText("TIME");
                title.SetText("SPEEDRUN LEVEL10");
                description.SetText("Player that slayed Broyon with the fastest way possible.");
                break;
            case LeaderboardType.SpeedRunLevel20:
                rankType.SetText("TIME");
                title.SetText("SPEEDRUN LEVEL20");
                description.SetText("Player that slayed Hell Fighter with the fastest way possible.");
                break;
            case LeaderboardType.Legacy:
                rankType.SetText("LEVEL");
                title.SetText("LEGACY LEGENDS");
                description.SetText("The old time glory from the player in version 1.1.0beta");
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }
    }

    private string DataToStringConvertion(int data, LeaderboardType type)
    {
        string rtn = data.ToString();
        switch (type)
        {
            case LeaderboardType.Level:
            case LeaderboardType.Legacy:
                // do nothing
                break;
            case LeaderboardType.SpeedRunLevel10:
                rtn = (data / 60).ToString() + "m" + (data % 60).ToString() + "s";
                break;
            case LeaderboardType.SpeedRunLevel20:
                rtn = (data / 60).ToString() + "m" + (data % 60).ToString() + "s";
                break;
            default:
                Debug.Log("<color=red>LEADERBOARD TYPE NOT FOUND</color>");
                break;
        }

        return rtn;
    }

    public void Refresh(Dictionary<string, string>[] data, int total)
    {
        listParent.sizeDelta = new Vector2(0, total * 40f + 70);
        head.anchoredPosition = new Vector3(0f, listParent.sizeDelta.y / 2f - head.sizeDelta.y / 2f + 22.5f, 0f);

        if (initialized)
        {
            ResetList();
        }
        Initialize(data, total);
    }

    public void ResetList()
    {
        for (int i = 0; i < ranklist.Length; i++)
        {
            if (ranklist[i] != null)
            {
                Destroy(ranklist[i].gameObject);
            }
        }
    }

    public void CheckDetailOpen(Dictionary<string, string> data)
    {
        detailCheckerPanel.gameObject.SetActive(true);
        detailCheckerPanel.DOScale(0.0f, 0.0f).SetUpdate(true);
        detailCheckerPanel.DOScale(1.0f, 0.5f).SetUpdate(true);
        detailCheckerAlpha.DOFade(0.5f, 0.5f).SetUpdate(true);

        string first,second;

        first = "HP: " + data["hp"] + " + " + data["hpregen"] + "/sec" + "\n";
        first += "Stamina: " + data["stamina"] + "\n";
        first += "Move Speed: " + data["movespeed"] + "\n";
        first += "AttackDamage: " + data["atkDmg"] + "\n";
        first += "Dash Damage: " + data["dashDmg"] + "\n";

        second = "Dash Cooldown: " + data["dashCD"].Substring(0, 4) + "sec \n";
        second += "Critical: " + data["critical"] + "\n";
        second += "Lifesteal: " + data["lifesteal"] + "% \n";
        second += "Lifedrain: " + data["lifedrain"] + "\n";
        
        detailCheckerTitle.SetText(data["name"]);
        detailCheckertext1.SetText(first);
        detailCheckertext2.SetText(second);
        versionText.SetText("from version "+ data["version"]);
    }
    public void CheckDetailClose()
    {
        detailCheckerPanel.gameObject.SetActive(true);
        detailCheckerPanel.DOScale(0.0f, 0.5f).SetUpdate(true);
        detailCheckerAlpha.DOFade(0.0f, 0.5f).SetUpdate(true);
    }

    public void SetValueScrollBar(float _time, float scrollbar)
    {
        leaderboardScrollValue.value = scrollbar;
        StartCoroutine(SetValueScrollBarLoop(_time, scrollbar));
    }

    IEnumerator SetValueScrollBarLoop(float _time, float scrollbar)
    {
        for (float time = _time; time > 0.0f; time -= Time.unscaledDeltaTime)
        {
            leaderboardScrollValue.value = scrollbar;
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        }
    }

    public void SetUnactiveLeaderboardButtonForSecond(float _time)
    {
        StartCoroutine(UnactiveLeaderboardButtonForSecondLoop(_time));
    }

    IEnumerator UnactiveLeaderboardButtonForSecondLoop(float _time)
    {
        menu.SetLeaderboardButtonClickable(false);
        yield return new WaitForSecondsRealtime(_time);
        menu.SetLeaderboardButtonClickable(true);
    }
}
