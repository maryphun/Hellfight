using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour
{
    
    [System.Serializable]
    enum InputType
    {
        Confirm,
        Cancel,
        OpenCloseMenu,
    }

    [SerializeField] InputType hotkey;
    [SerializeField] UnityEvent events;

    PlayerAction _input;

    private void Awake()
    {
        _input = new PlayerAction();
        if (hotkey == InputType.Confirm)
        {
            _input.MenuControls.Confirm.performed += ctx => ClickTrigger();
        }
        if (hotkey == InputType.Cancel)
        {
            _input.MenuControls.Cancel.performed += ctx => ClickTrigger();
        }
        if (hotkey == InputType.OpenCloseMenu)
        {
            _input.MenuControls.OpenCloseMenu.performed += ctx => ClickTrigger();
        }
    }
    private void OnEnable()
    {
        _input.Enable();
    }

    private void OnDisable()
    {
        _input.Disable();
    }

    public void ClickTrigger()
    {
        if (!enabled) return;
        events.Invoke();
    }
}
