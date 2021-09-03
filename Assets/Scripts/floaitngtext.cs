using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class floaitngtext : MonoBehaviour
{
    float timeLeft, speed;
    bool activated = false;
    Vector2 direction;
    [SerializeField]TMP_Text txt;

    public void Initialize(float time, float _speed, string text, Color color, Vector2 _direction, float size)
    {
        txt.SetText(text);
        txt.color = color;
        txt.DOFade(0.0f, time);
        txt.fontSize = size / 70f;
        timeLeft = time;
        speed = _speed / 100f;
        direction = _direction.normalized;
        activated = true;
        enabled = true;
    }

    private void Update()
    {
        if (!activated)
        {
            enabled = false;
            return;
        }

        transform.DOMove(new Vector2(transform.position.x, transform.position.y) + direction * speed * Time.deltaTime, Time.deltaTime, false);

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0.0f)
        {
            Destroy(gameObject);
        }
    }
}
