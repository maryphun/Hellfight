using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class StaminaText : MonoBehaviour
{
    [SerializeField] Controller player;

    TMP_Text text;
    RectTransform thisUI;
    bool isShaking = false;
    Coroutine shake;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TMP_Text>();
        thisUI = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        text.SetText(player.GetCurrentStamina().ToString() + "/" + player.GetMaxStamina().ToString());
    }
    public void StartShake(int count)
    {
        if (isShaking)
        {
            StopCoroutine(shake);
            isShaking = false;
            thisUI.DORewind();
        }

        shake = StartCoroutine(ShakeBar(1, count));
    }

    IEnumerator ShakeBar(float magnitude, int count)
    {
        thisUI.DORewind();
        isShaking = true;

        for (int i = 0; i < count; i++)
        {
            thisUI.DOShakePosition(0.1f, magnitude, 10, 90, false, true);
            yield return new WaitForSeconds(0.1f);
        }

        isShaking = false;
    }
}
