using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ControlPattern : Singleton<ControlPattern>
{
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
        FBPP.SetInt("ControlPattern", (int)pattern);
    }

    public CtrlPattern GetControlPattern()
    {
        return controlPattern;
    }

    public void DetectControlPattern()
    {
        if (hardSettedControlMethod)
        {
            return;
        }

        // check option setting
        CtrlPattern option = (CtrlPattern)FBPP.GetInt("ControlPattern", -1);
        if (option != CtrlPattern.NULL)
        {
            controlPattern = option;
            hardSettedControlMethod = true;
            return;
        }

        // If there are no joystick connected, automatically decide keyboard control
        if (!ControlPattern.Instance().IsJoystickConnected())
        {
            // there are no joystick registered
            controlPattern = CtrlPattern.KEYBOARD;
            Debug.Log("No joystick connected. Assume player use keyboard as main input method.");
            return;
        }
        else
        {
            StartCoroutine(DetectNextKey());
        }
    }

    IEnumerator DetectNextKey()
    {
        if (!GetJoystickAnyKey())
        {
            // no joystick input
            if (!Input.anyKey)
            {
                // no keyboard input either
                yield return new WaitForFixedUpdate();
                StartCoroutine(DetectNextKey());
            }
            else
            {
                // keyboard control detected
                controlPattern = CtrlPattern.KEYBOARD;
                Debug.Log("Joystick is connected but player use keyboard to control.");
            }
        }
        else
        {
            // player is clicking buttons on joystick
            controlPattern = CtrlPattern.JOYSTICK;
            Debug.Log("Joystick is connected and player use joystick as control method.");
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
