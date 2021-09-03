using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class projectile : MonoBehaviour
{
    Vector2 moveDirect;
    float speed;
    int dmg;
    Controller player;
    float collideRange;
    [SerializeField] Color particleColor;

    [SerializeField] GameObject onHitEffect;

    public void Initialize(Vector2 moveDirection, float _speed, int damage, Controller _player, float _collideRange)
    {
        moveDirect = moveDirection.normalized;
        speed = _speed;
        dmg = damage;
        player = _player;
        collideRange = _collideRange;

        GetComponent<SpriteRenderer>().flipX = (moveDirection.x > 0);

        transform.SetParent(GameObject.Find("World").transform);
    }

    private void Update()
    {
        // move
        transform.DOMove(new Vector2(transform.position.x, transform.position.y) + (moveDirect * speed * Time.deltaTime), 0.0f, false);

        // check collision
        if (Mathf.Abs(player.transform.position.x - transform.position.x) < collideRange
            && Mathf.Abs(player.transform.position.y - transform.position.y) < collideRange * 2f)
        {
            player.DealDamage(dmg, transform);
            if (onHitEffect != null)
            {
                GameObject tmp;
                tmp = Instantiate(onHitEffect, transform.position, Quaternion.identity);
                tmp.GetComponent<ParticleScript>().SetParticleColor(particleColor);
            }
            Destroy(gameObject);
        }

        // delete
        if (Mathf.Abs(transform.position.x ) > 10.0f)
        {
            Destroy(gameObject);
        }
    }
}
