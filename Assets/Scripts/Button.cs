using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour
{
    [SerializeField] KeyCode hotkey;
    [SerializeField] string keypadhotkey;
    [SerializeField] UnityEvent events;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(hotkey) || Input.GetButtonDown(keypadhotkey))
        {
            ClickTrigger();
        }
    }

    public void ClickTrigger()
    {
        events.Invoke();
    }
}
