using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class hpbar : MonoBehaviour
{
    [SerializeField] Controller player;
    [SerializeField] Image fill;
    [SerializeField] Image delayfill;
    [SerializeField] private float maxSize = 1.0f;
    SpriteRenderer sprite;
    RectTransform rectTransform;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();
        //GetComponent<RectTransform>().DOScaleX((player.GetMaxHP() / 100.0f) * maxSize, 0.0f);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        SetRectTransformLeft(rectTransform, Mathf.MoveTowards(rectTransform.offsetMin.x, 400f - Mathf.Min((float)player.GetMaxHP() * 3f, 300.0f), 1.0f));
        SetRectTransformRight(rectTransform, Mathf.MoveTowards(-rectTransform.offsetMax.x, 400f - Mathf.Min((float)player.GetMaxHP() * 3f, 300.0f), 1.0f));
        //GetComponent<RectTransform>().DOScaleX(Mathf.Min((player.GetMaxHP() / 100.0f) * maxSize, maxSize), 0.5f);
        fill.DOFillAmount((float)player.GetCurrentHP() / (float)player.GetMaxHP(), 0.5f);
        delayfill.DOFillAmount(fill.fillAmount, 0.5f);
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
