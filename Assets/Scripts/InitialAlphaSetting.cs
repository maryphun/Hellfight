using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InitialAlphaSetting : MonoBehaviour
{
    // This script is to make UI develop easier.
    [SerializeField] Color initialColor;
    [SerializeField] bool isActiveAtStart;
    
    void Awake()
    {
        var img = GetComponent<Image>();
        img.color = initialColor;

        enabled = isActiveAtStart;
    }
}
