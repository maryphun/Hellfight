using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] Controller player;
    [SerializeField] Image fill;
    [SerializeField] private Image delayfill;
    [SerializeField] private float maxSize = 1.0f;
    SpriteRenderer sprite;
    RectTransform rectTransform;

    [SerializeField] Color normalColor;
    [SerializeField] Color lowColor;

    bool onLowColor = false;

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();
        onLowColor = false;
    }

    void Update()
    {
        SetRectTransformLeft(rectTransform, Mathf.MoveTowards(rectTransform.offsetMin.x, 400f - Mathf.Min((float)player.GetMaxStamina() * 1.5f, 300.0f), 1.0f));
        SetRectTransformRight(rectTransform, Mathf.MoveTowards(-rectTransform.offsetMax.x, 400f - Mathf.Min((float)player.GetMaxStamina() * 1.5f, 300.0f), 1.0f));
        fill.DOFillAmount((float)player.GetCurrentStamina() / (float)player.GetMaxStamina(), 1.0f);
        delayfill.DOFillAmount(fill.fillAmount, 0.5f);

        if (!onLowColor)
        {
            if (fill.fillAmount < 0.2f)
            {
                fill.DOColor(lowColor, 0.5f);
                onLowColor = true;
            }
        }
        else
        {
            if ((float)player.GetCurrentStamina() / (float)player.GetMaxStamina() > 0.2f)
            {
                fill.DOColor(normalColor, 0.5f);
                onLowColor = false;
            }
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
