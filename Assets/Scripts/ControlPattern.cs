using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ControlPattern : Singleton<ControlPattern>
{
    UnityEvent callback;

    public enum CtrlPattern
    {
        NULL = -1,
        KEYBOARD,
        JOYSTICK,
    }

    private bool hardSettedControlMethod;
    private CtrlPattern controlPattern;

    public void RegisterControlPattern(CtrlPattern pattern)
    {
        controlPattern = pattern;
        hardSettedControlMethod = true;
        PlayerPrefs.SetInt("ControlPattern", (int)pattern);
    }

    public CtrlPattern GetControlPattern()
    {
        return controlPattern;
    }

    public void DetectControlPattern(UnityEvent callback)
    {
        if (hardSettedControlMethod)
        {
            callback.Invoke();
            return;
        }

        // check option setting
        CtrlPattern option = (CtrlPattern)PlayerPrefs.GetInt("ControlPattern", -1);
        if (option != CtrlPattern.NULL)
        {
            controlPattern = option;
            hardSettedControlMethod = true;

            callback.Invoke();
            return;
        }

        // If there are no joystick connected, automatically decide keyboard control
        if (!ControlPattern.Instance().IsJoystickConnected())
        {
            // there are no joystick registered
            controlPattern = CtrlPattern.KEYBOARD;
            Debug.Log("keyboard!");

            callback.Invoke();
            return;
        }
        else
        {
            StartCoroutine(DetectNextKey(callback));
        }
    }

    IEnumerator DetectNextKey(UnityEvent callback)
    {
        if (!GetJoystickAnyKey())
        {
            // no joystick input
            if (!Input.anyKey)
            {
                // no keyboard input either
                yield return new WaitForFixedUpdate();
                StartCoroutine(DetectNextKey(callback));
            }
            else
            {
                // keyboard control detected
                controlPattern = CtrlPattern.KEYBOARD;
                Debug.Log("KEYBOARD!");
                callback.Invoke();
            }
        }
        else
        {
            // player is clicking buttons on joystick
            controlPattern = CtrlPattern.JOYSTICK;
            Debug.Log("JOYSTICK!");
            callback.Invoke();
        }
    }

    public bool GetJoystickAnyKey()
    {
        return Input.GetButton("Spell") || Input.GetButton("Attack") || Input.GetButton("Dash") || Input.GetButton("Start") || Input.GetButton("Jump") ||
                Input.GetAxisRaw("JoyPadHorizontal") != 0 || Input.GetAxisRaw("JoyPadVertical") != 0;
    }

    public bool IsJoystickConnected()
    {
        return Input.GetJoystickNames().Length > 0;
    }
}
