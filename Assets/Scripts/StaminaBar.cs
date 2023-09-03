using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// スタミナバーを管理するクラス
/// </summary>
public class StaminaBar : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Controller player;
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text heal;
    [SerializeField] private Image delayfill;
    [SerializeField] private float maxSize = 1.0f;

    [Header("Setting")]
    [SerializeField] private Color normalColor;
    [SerializeField] private Color slowedColor;
    [SerializeField] private Color lowColor;
    [SerializeField, Range(0.0f, 1.0f)] private float lowThreshold = 0.20f;
    [SerializeField, Range(0.0f, 1.0f)] private float colorChangetime = 0.50f;
    [SerializeField, Range(0.0f, 1.0f)] private float fillTime = 0.50f;

    private SpriteRenderer sprite;
    private RectTransform rectTransform;


    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();
    }

    void FixedUpdate()
    {
        //　MAXスタミナ量によってバーの長さを変化させる
        SetRectTransformLeft(rectTransform, Mathf.MoveTowards(rectTransform.offsetMin.x, 400f - Mathf.Min((float)player.GetMaxStamina() * 1.5f, 300.0f), 100.0f * Time.fixedDeltaTime));
        SetRectTransformRight(rectTransform, Mathf.MoveTowards(-rectTransform.offsetMax.x, 400f - Mathf.Min((float)player.GetMaxStamina() * 1.5f, 300.0f), 100.0f * Time.fixedDeltaTime));

        // スタミナを反映
        fill.DOFillAmount((float)player.GetCurrentStamina() / (float)player.GetMaxStamina(), fillTime);
        delayfill.DOFillAmount(fill.fillAmount, fillTime);

        bool isSlowed = player.IsStaminaRegenerateSlowed();
        bool isLowColor = (float)player.GetCurrentStamina() / (float)player.GetMaxStamina() < lowThreshold;
        if (isLowColor)
        {
            fill.DOColor(lowColor, colorChangetime);
        }
        else if (isSlowed)
        {
            fill.DOColor(slowedColor, colorChangetime);
        }
        else
        {
            fill.DOColor(normalColor, colorChangetime);
        }

        if (isSlowed)
        {
            heal.SetText(string.Empty);
        }
        else
        {
            // スタミナ増加中は　+　サインを表示する
            heal.SetText("+");
        }
    }

    public void SetRectTransformLeft(RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public void SetRectTransformRight(RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }
}
