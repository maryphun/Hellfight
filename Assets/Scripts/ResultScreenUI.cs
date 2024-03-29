using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ResultScreenUI : MonoBehaviour
{
    [Header("Book UI")]
    [Header("References")]
    [SerializeField] GameObject titleUI;
    [SerializeField] RectTransform bookUI;
    [SerializeField] TMP_Text levelReachValue;
    [SerializeField] TMP_Text monsterCntValue;
    [SerializeField] TMP_Text bossCntValue;
    [SerializeField] TMP_Text totalTimeValue;
    [SerializeField] TMP_Text averageTimeValue;
    [SerializeField] Image resultStamp;

    [Header("Progress Bar")]
    [SerializeField] RectTransform progressBar;
    [SerializeField] Image progressBarFill;

    [Header("StampSprites")]
    [SerializeField] Sprite A;
    [SerializeField] Sprite B;
    [SerializeField] Sprite C;
    [SerializeField] Sprite D;
    [SerializeField] Sprite F;

    private bool isFinished = false;
    private GameManager gameMng;

    enum ResultPerformance
    {
        A,
        B,
        C,
        D,
        F,

        Max,
    }

    // pass reference
    public void Initialization(GameManager reference)
    {
        gameMng = reference;
    }

    public void ResetUI()
    {
        isFinished = false;

        levelReachValue.text = string.Empty;
        monsterCntValue.text = string.Empty;
        bossCntValue.text = string.Empty;
        totalTimeValue.text = string.Empty;
        averageTimeValue.text = string.Empty;
        resultStamp.color = new Color(1, 1, 1, 0);

        progressBarFill.fillAmount = ProgressManager.Instance().GetProgressPoint() / ProgressManager.Instance().GetPointRequiredToNextLevel();

        titleUI.gameObject.SetActive(false);
        bookUI.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
    }

    public void StartAnimation(Result result)
    {
        isFinished = false;
        StartCoroutine(ResultAnimation(result));
    }

    IEnumerator ResultAnimation(Result result)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        titleUI.gameObject.SetActive(true);
        bookUI.gameObject.SetActive(true);
        progressBar.gameObject.SetActive(true);
        // LEVEL REACH
        for (int i = 0; i <= result.levelReached; i++)
        {
            levelReachValue.text = i.ToString();
            AudioManager.Instance.PlaySFX("Ui Bleep", 0.1f);
            if (i > 0) AddProggressPoint(40, 0.08f);
            yield return new WaitForSecondsRealtime(0.08f);
        }
        AudioManager.Instance.PlaySFX("collect");
        TextEffect(levelReachValue, 0.3f);

        yield return new WaitForSecondsRealtime(0.5f);

        // MONSTER SLAYED
        for (int i = 0; i <= result.monsterSlayed; i++)
        {
            monsterCntValue.text = i.ToString();
            AudioManager.Instance.PlaySFX("Ui Bleep", 0.1f);
            if (i > 0) AddProggressPoint(25, 0.04f);
            yield return new WaitForSecondsRealtime(0.04f);
        }
        AudioManager.Instance.PlaySFX("collect");
        TextEffect(monsterCntValue, 0.3f);

        yield return new WaitForSecondsRealtime(0.5f);

        // BOSS SLAYED
        for (int i = 0; i <= result.bossSlayed; i++)
        {
            bossCntValue.text = i.ToString();
            AudioManager.Instance.PlaySFX("Ui Bleep", 0.2f);
            if (i > 0) AddProggressPoint(330, 0.3f);
            yield return new WaitForSecondsRealtime(0.3f);
        }
        AudioManager.Instance.PlaySFX("collect");
        TextEffect(bossCntValue, 0.3f);

        yield return new WaitForSecondsRealtime(0.5f);

        // TOTAL TIME VALUE
        for (int i = 0; i <= result.totalTime; Mathf.Min(i+=Random.Range(5,15), result.totalTime))
        {
            totalTimeValue.text = SecondToTimeString(i);
            AudioManager.Instance.PlaySFX("Ui Bleep", 0.1f);
            yield return new WaitForSecondsRealtime(0.03f);
        }
        AudioManager.Instance.PlaySFX("collect");
        TextEffect(totalTimeValue, 0.3f);

        yield return new WaitForSecondsRealtime(0.5f);

        // TOTAL TIME VALUE
        int avgTime = result.GetAverageTimePerLevel();
        for (int i = 0; i <= avgTime; Mathf.Min(i += Random.Range(5, 15), avgTime))
        {
            averageTimeValue.text = SecondToTimeString(i);
            AudioManager.Instance.PlaySFX("Ui Bleep", 0.1f);
            yield return new WaitForSecondsRealtime(0.03f);
        }
        AudioManager.Instance.PlaySFX("collect");
        TextEffect(averageTimeValue, 0.3f);

        yield return new WaitForSecondsRealtime(0.8f);

        // Calculate result performance
        int progressPointReward = 0;
        ResultPerformance evaluation = CalculateResultPerformanceEvaluation(result);
        switch (evaluation)
        {
            case ResultPerformance.A:
                resultStamp.sprite = A;
                progressPointReward = 1000;
                break;
            case ResultPerformance.B:
                resultStamp.sprite = B;
                progressPointReward = 500;
                break;
            case ResultPerformance.C:
                resultStamp.sprite = C;
                progressPointReward = 300;
                break;
            case ResultPerformance.D:
                resultStamp.sprite = D;
                progressPointReward = 150;
                break;
            case ResultPerformance.F:
                resultStamp.sprite = F;
                progressPointReward = 50;
                break;
        }

        resultStamp.color = new Color(1, 1, 1, 1);
        resultStamp.GetComponent<RectTransform>().localScale = new Vector3(2f, 2f, 1f);
        resultStamp.GetComponent<RectTransform>().DOScale(new Vector3(1, 1, 1), 0.4f).SetUpdate(true);

        AudioManager.Instance.PlaySFX("stamp", 0.5f);
        yield return new WaitForSecondsRealtime(0.4f);
        AddProggressPoint(progressPointReward, 0.5f);

        // call this only once so it doesn't write all the time
        FBPP.Save();
        FBPP.GetSaveFileAsJson(); // backup to avoid data lost

        bookUI.DOShakePosition(0.5f, 3, 100, 90, false, true).SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.25f);
        AudioManager.Instance.PlaySFX("Grade_" + evaluation.ToString());

        StartCoroutine(WaitForInput());
    }

    void TextEffect(TMP_Text text, float time)
    {
        Color originalColor = text.color;
        text.color = new Color(1, 1, 1, 1);
        text.DOColor(originalColor, time).SetUpdate(true);
    }

    string SecondToTimeString(int second)
    {
        return "Time - " + (second / 60).ToString() + ":" + (second % 60).ToString("00");
    }

    private ResultPerformance CalculateResultPerformanceEvaluation(Result result)
    {
        int point = 0;

        point += result.levelReached * 5;
        if (result.levelReached > 5) point += 15;
        if (result.levelReached > 15) point += 15;
        if (result.levelReached > 25) point += 15;
        point += result.monsterSlayed * 3;
        point += result.bossSlayed * 25;
        point = Mathf.FloorToInt((float)point / ((float)result.GetAverageTimePerLevel() * 0.01f));

        if (point > 100) return ResultPerformance.A;
        if (point > 80) return ResultPerformance.B;
        if (point > 60) return ResultPerformance.C;
        if (point > 40) return ResultPerformance.D;
        return ResultPerformance.F;
    }

    private void AddProggressPoint(int newPoint, float animTime)
    {
        // calculation
        int lvl = ProgressManager.Instance().GetUnlockLevel();
        int pts = ProgressManager.Instance().GetProgressPoint();

        pts += newPoint;

        if (pts >= ProgressManager.Instance().GetPointRequiredToNextLevel())
        {
            pts = pts % ProgressManager.Instance().GetPointRequiredToNextLevel();
            lvl++;
        }

        // update new progress data (save to cache)
        //@todo reduce call to only once
        ProgressManager.Instance().UpdateProgress(pts, lvl, false);
        
        // UI
        StartCoroutine(ProgressBarFillAnimation(((float)pts / (float)ProgressManager.Instance().GetPointRequiredToNextLevel()), animTime));
    }

    IEnumerator ProgressBarFillAnimation(float fillTarget, float totalTime)
    {
        if (fillTarget >= progressBarFill.fillAmount)
        {
            progressBarFill.DOFillAmount(fillTarget, totalTime).SetUpdate(true);
        }
        else // level up
        {
            progressBarFill.DOFillAmount(1.0f, totalTime * 0.3f).SetUpdate(true);

            yield return new WaitForSecondsRealtime(totalTime * 0.3f);

            progressBar.DOShakePosition(0.5f, 3, 100, 90, false, true).SetUpdate(true);
            Color col = progressBarFill.color;
            progressBarFill.DOColor(new Color(col.r,col.g,col.b,0), totalTime * 0.2f).SetUpdate(true);
            AudioManager.Instance.PlaySFX("UnlockLevelUp", 1.5f);

            yield return new WaitForSecondsRealtime(totalTime * 0.2f);

            progressBarFill.DOComplete();
            progressBarFill.color = col;
            progressBarFill.fillAmount = 0.0f;
            progressBarFill.DOFillAmount(fillTarget, totalTime * 0.5f).SetUpdate(true);
        }
    }

    /// <summary>
    /// 入力待ち
    /// </summary>
    IEnumerator WaitForInput()
    {
        while (!gameMng.IsConfirmKeyPressed())
        {
            yield return null;
        }
        gameMng.ResetConfirmKey(); // reset input

        // FADE OUT
        // TODO: リザルトシーンをフェイドアウトさせる

        ResetUI();
        isFinished = true;

        titleUI.gameObject.SetActive(false);
        bookUI.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// アニメーション完了か
    /// </summary>
    public bool IsFinished()
    {
        return isFinished;
    }
}
