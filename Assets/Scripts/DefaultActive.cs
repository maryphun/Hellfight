using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultActive : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] bool isActiveByDefault;

    // Start is called before the first frame update
    private void Start()
    {
        gameObject.SetActive(isActiveByDefault);
    }
}
