using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Assets.SimpleLocalization;

[RequireComponent(typeof(TMP_Text))]
public class LocalizationTMP : MonoBehaviour
{
    public string LocalizationKey;

    public void Start()
    {
        Localize();
        LocalizationManager.LocalizationChanged += Localize;
    }

    public void OnDestroy()
    {
        LocalizationManager.LocalizationChanged -= Localize;
    }

    private void Localize()
    {
        GetComponent<TMP_Text>().SetText(LocalizationManager.Localize(LocalizationKey));
    }
}
