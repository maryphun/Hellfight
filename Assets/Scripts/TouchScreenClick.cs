using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class TouchScreenClick : MonoBehaviour
{
    private Image img;
    public bool clicked;
    public bool released;
    public bool holding;
    private void Start()
    {
        img = GetComponent<Image>();
    }

    public void Clicked()
    {
        holding = true;
        clicked = true;
        img.DOComplete();
        img.color = new Color(0.25f, 0.25f, 0.25f, 0.78f);
    }

    public void Released()
    {
        released = true;
        holding = false;
        img.DOColor(new Color(1f, 1f, 1f, 0.78f), 0.4f);
    }

    private void LateUpdate()
    {
        released = false;
        clicked = false;
    }
}
