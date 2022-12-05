using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.SimpleLocalization;

public class CreditsPage : MonoBehaviour
{
    [SerializeField] Transform previousbutton;
    [SerializeField] Menu menu;

    [Header("Speites")]
    [SerializeField] Sprite homeButtonSprite;
    [SerializeField] Sprite homeButtonSpritePressed;

    public PlayerAction _input;

    private void Awake()
    {
        // INPUT SYSTEM
        _input = new PlayerAction();
        _input.MenuControls.LeaderboardMove.performed += ctx => InputLeftRight(ctx.ReadValue<float>());
    }
    private void OnEnable()
    {
        _input.Enable();

        UpdateButton();
    }
    private void OnDisable()
    {
        _input.Disable();
    }
    private void UpdateButton()
    {
        previousbutton.GetComponent<Image>().sprite = homeButtonSprite;

        // change pressed sprite of button
        SpriteState spriteState = new SpriteState();
        spriteState = previousbutton.GetComponent<Selectable>().spriteState;
        spriteState.pressedSprite = homeButtonSpritePressed;
        previousbutton.GetComponent<Selectable>().spriteState = spriteState;
    }

    public void InputLeftRight(float value)
    {
        if (value < 0)
        {
            // left
            if (previousbutton.gameObject.activeInHierarchy)
            {
                // previousbutton.GetComponent<Button>().ClickTrigger();
                BackToMainMenu();
            }
        }
    }

    private void BackToMainMenu()
    {
        // back to main menu page
        menu.CreditsBackToMainMenu();
    }
}
