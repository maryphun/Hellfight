using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ScreenTitle : MonoBehaviour
{
    Vector2 originalPos;
    RectTransform rectT;
    Coroutine loop;
    bool coroutineRunning;

    [SerializeField] Vector2 moveVec;
    [SerializeField] float time;

    private void Awake()
    {
        rectT = GetComponent<RectTransform>();
        originalPos = rectT.anchoredPosition;
    }

    private void OnEnable()
    {
        if (coroutineRunning)  StopCoroutine(loop);
        rectT.anchoredPosition = originalPos - moveVec;
        loop = StartCoroutine(animateLoop(moveVec));
    }

    IEnumerator animateLoop(Vector2 vector)
    {
        coroutineRunning = true;
        rectT.DOAnchorPos(originalPos + vector, time, true);
        yield return new WaitForSecondsRealtime(time);
        rectT.DOAnchorPos(originalPos - vector, time, true);
        yield return new WaitForSecondsRealtime(time);
        coroutineRunning = false;
        loop = StartCoroutine(animateLoop(vector));
    }
}
