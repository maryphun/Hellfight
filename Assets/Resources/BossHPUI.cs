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
    [SerializeField] Image frame;
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

    public void ChangeFrameColor(Color color)
    {
        frame.color = new Color(color.r, color.g, color.b, frame.color.a);
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
        transform.DOComplete();
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
                // shake the bar if boss is damaged
                if (lastPercentage > registeredUnit.GetCurrentHPPercentage())
                {
                    transform.DOComplete();
                    if (Mathf.Abs(lastPercentage - registeredUnit.GetCurrentHPPercentage()) < 0.05f)
                    {
                        // smaller shake
                        transform.DOShakePosition(0.3f, 4, 75, 90, false, true);
                    }
                    else
                    {
                        transform.DOShakePosition(0.5f, 8, 150, 90, false, true);
                    }
                }

                // lerp the fill amount if boss is healed
                float fillTime = 0.0f;
                if (lastPercentage < registeredUnit.GetCurrentHPPercentage())
                {
                    fillTime = 1.0f;
                }

                fillDelay.DOComplete();
                lastPercentage = registeredUnit.GetCurrentHPPercentage();
                fill.DOFillAmount(registeredUnit.GetCurrentHPPercentage(), fillTime);
                fillDelay.DOFillAmount(registeredUnit.GetCurrentHPPercentage(), 1.0f);
            }
        }
        else
        {
            Deactivate();
        }
    }
}
