using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] Controller player;
    [SerializeField] Image fill;
    [SerializeField] TMP_Text heal;
    [SerializeField] private Image delayfill;
    [SerializeField] private float maxSize = 1.0f;
    SpriteRenderer sprite;
    RectTransform rectTransform;

    [SerializeField] Color normalColor;
    [SerializeField] Color slowedColor;
    [SerializeField] Color lowColor;

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();
    }

    void FixedUpdate()
    {
        SetRectTransformLeft(rectTransform, Mathf.MoveTowards(rectTransform.offsetMin.x, 400f - Mathf.Min((float)player.GetMaxStamina() * 1.5f, 300.0f), 100.0f * Time.fixedDeltaTime));
        SetRectTransformRight(rectTransform, Mathf.MoveTowards(-rectTransform.offsetMax.x, 400f - Mathf.Min((float)player.GetMaxStamina() * 1.5f, 300.0f), 100.0f * Time.fixedDeltaTime));
        fill.DOFillAmount((float)player.GetCurrentStamina() / (float)player.GetMaxStamina(), 0.5f);
        delayfill.DOFillAmount(fill.fillAmount, 0.5f);

        bool isSlowed = player.IsStaminaRegenerateSlowed();
        bool isLowColor = (float)player.GetCurrentStamina() / (float)player.GetMaxStamina() < 0.20f;
        if (isLowColor)
        {
            fill.DOColor(lowColor, 0.5f);
        }
        else if (isSlowed)
        {
            fill.DOColor(slowedColor, 0.5f);
        }
        else
        {
            fill.DOColor(normalColor, 0.5f);
        }

        //heal.rectTransform.DOAnchorPosX(Mathf.Min((-rectTransform.offsetMax.x/2f) - 10f, 400.0f), 0.8f);
        if (isSlowed)
        {
            heal.SetText(string.Empty);
        }
        else
        {
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
