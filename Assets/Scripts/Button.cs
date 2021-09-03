using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour
{
    [SerializeField] KeyCode hotkey;
    [SerializeField] UnityEvent events;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(hotkey))
        {
            ClickTrigger();
        }
    }

    public void ClickTrigger()
    {
        events.Invoke();
    }
}
