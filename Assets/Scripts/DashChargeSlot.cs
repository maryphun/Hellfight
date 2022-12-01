using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DashChargeSlot : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] Image fade;
    [SerializeField] Image frame;

    bool isUsed;

    public void Use()
    {
        if (isUsed) return;

        isUsed = true;
        fill.DOComplete();
        StartCoroutine(UseCoroutine());
    }

    public void Recover(float percentage, float time)
    {
        fill.DOFillAmount(percentage, time);

        if (percentage == 1.0f)
        {
            isUsed = false;
        }
    }

    IEnumerator UseCoroutine()
    {
        fade.color = new Color(1, 1, 1, 1);
        yield return new WaitForSeconds(0.1f);
        fade.DOColor(new Color(1, 1, 1, 0), 0.2f);
        yield return new WaitForSeconds(0.1f);
        fill.fillAmount = 0.0f;
    }

    public bool IsUsed()
    {
        return isUsed;
    }
}
