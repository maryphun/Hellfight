using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BackgroundScroll : MonoBehaviour
{
    [SerializeField] Controller player;
    [SerializeField, Range(0f, 1f)] float scrollScale;
    [SerializeField, Range(0f, 1f)] float scrollScaleY;
    float originalX, originalY;
    private void Start()
    {
        originalY = transform.position.y;
        originalX = transform.position.x;
    }

    private void Update()
    {
        transform.DOMoveX(originalX - player.transform.position.x * scrollScale, 0.1f, false);
        transform.DOMoveY(originalY - (player.transform.position.y + 1.444933f) * scrollScaleY, 0.1f, false);
    }
}
