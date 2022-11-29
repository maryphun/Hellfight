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
    bool isShaking = false;
    float lastRoateDir = 1.0f;
    Coroutine shake;

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
        SetRectTransformLeft(rectTransform, Mathf.MoveTowards(rectTransform.offsetMin.x, 400f - Mathf.Min((float)player.GetMaxHP() * 3f, 300.0f), 100.0f * Time.fixedDeltaTime));
        SetRectTransformRight(rectTransform, Mathf.MoveTowards(-rectTransform.offsetMax.x, 400f - Mathf.Min((float)player.GetMaxHP() * 3f, 300.0f), 100.0f * Time.fixedDeltaTime));
        //GetComponent<RectTransform>().DOScaleX(Mathf.Min((player.GetMaxHP() / 100.0f) * maxSize, maxSize), 0.5f);
        fill.DOFillAmount((float)player.GetCurrentHP() / (float)player.GetMaxHP(), 0.5f);
        delayfill.DOFillAmount(fill.fillAmount, 0.5f);

        // less than 10%
        if (((float)player.GetCurrentHP() / (float)player.GetMaxHP()) < 0.1f
            && player.GetCurrentHP() >= 1
            && player.gameObject.activeSelf)
        {
            if (!isShaking)
            {
                StartShake(2);
            }
        }
        else
        {
            rectTransform.rotation = Quaternion.Euler(0, 0, 0);
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

    public void StartShake(int count)
    {
        if (isShaking)
        {
            StopCoroutine(shake);
            isShaking = false;
            rectTransform.rotation = Quaternion.Euler(0, 0, 0);
        }

        shake = StartCoroutine(ShakeBar(0.15f, count));
    }

    IEnumerator ShakeBar(float magnitude, int count)
    {
        rectTransform.DORewind();
        isShaking = true;

        for (int i = 0; i < count; i++)
        {
            lastRoateDir = lastRoateDir > 0.0f ? -1.0f : 1.0f; 
            rectTransform.DORotate(new Vector3(0.0f, 0.0f, lastRoateDir * magnitude), 0.05f, RotateMode.Fast);
            yield return new WaitForSeconds(0.05f);
            rectTransform.DORotate(new Vector3(0.0f, 0.0f, 0.0f), 0.05f, RotateMode.Fast);
            yield return new WaitForSeconds(0.05f);
            rectTransform.rotation = Quaternion.Euler(0, 0, 0);
        }

        isShaking = false;
    }
}
