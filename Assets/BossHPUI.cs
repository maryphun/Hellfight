using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BossHPUI : MonoBehaviour
{
    [SerializeField] float hidePosY = 250.0f;
    [SerializeField] float showPosY = 180.0f;
    [SerializeField] Image fill;
    [SerializeField] Image fillDelay;
    [SerializeField] TMP_Text name;
    [SerializeField] CanvasGroup canvasGroup;

    bool isActivated;
    EnemyControl registeredUnit;
    float lastPercentage = 1.0f;

    public void Initialize()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, hidePosY);
        enabled = false;
        isActivated = false;
        canvasGroup.alpha = 0.0f;
        name.text = string.Empty;
    }

    public void Activate(EnemyControl enemy)
    {
        isActivated = true;
        enabled = true;
        name.SetText(enemy.GetName());
        canvasGroup.alpha = 0.0f;
        transform.DOLocalMoveY(showPosY, 2.5f);
        canvasGroup.DOFade(1.0f, 5.0f);
        registeredUnit = enemy;

        lastPercentage = enemy.GetCurrentHPPercentage();
        fill.fillAmount = enemy.GetCurrentHPPercentage();
        fillDelay.fillAmount = enemy.GetCurrentHPPercentage();
    }

    public void Deactivate()
    {
        isActivated = false;
        enabled = false;
        transform.DOLocalMoveY(hidePosY, 2.5f);
        canvasGroup.DOFade(0.0f, 0.5f);
    }

    private void Update()
    {
        if (!isActivated) return;

        if (registeredUnit.IsAlive())
        {
            if (registeredUnit.GetCurrentHPPercentage() != lastPercentage)
            {
                fillDelay.DOComplete();
                lastPercentage = registeredUnit.GetCurrentHPPercentage();
                fill.fillAmount = registeredUnit.GetCurrentHPPercentage();
                fillDelay.DOFillAmount(registeredUnit.GetCurrentHPPercentage(), 1.0f);
            }
        }
        else
        {
            Deactivate();
        }
    }
}
