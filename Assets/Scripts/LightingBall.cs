using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LightingBall : MonoBehaviour
{
    GameManager gameMng;
    Controller player;
    PakuAI master;
    int shootLeft;
    float shootInterval;

    int dmg, maxDmg;

    float shootTimeCnt;
    private GameObject electricSparkEffect;
    private GameObject lightningSparkEffect;
    private GameObject lightningStrikeEffect;

    private void Awake()
    {
        electricSparkEffect = Resources.Load("Prefabs/ElectricSpark") as GameObject;
        lightningSparkEffect = Resources.Load("Prefabs/LightningSpark") as GameObject;
        lightningStrikeEffect = Resources.Load("Prefabs/LightningStrike") as GameObject;
        enabled = false;
    }

    public void Initialize(GameManager _gameMng, Controller _player, PakuAI _master, int shoot, float _shootInterval, int damage, int maxDamage)
    {
        gameMng = _gameMng;
        player = _player;
        master = _master;
        shootLeft = shoot;
        shootInterval = _shootInterval;
        shootTimeCnt = _shootInterval;
        dmg = damage;
        maxDmg = maxDamage;

        enabled = true;

        // Move to target height
        transform.DOLocalMoveY(1.5f, shootInterval);
    }

    public void DestroySelf(float time)
    {
        master.ElectricBallUnRegister(this);

        GameObject tmp = Instantiate(electricSparkEffect, transform.position, Quaternion.identity);

        tmp.transform.SetParent(transform.parent);

        Destroy(gameObject, time + Time.deltaTime);
    }

    public void DestroyWithPaku()
    {
        GameObject tmp = Instantiate(electricSparkEffect, transform.position, Quaternion.identity);

        tmp.transform.SetParent(transform.parent);

        Destroy(gameObject);
    }

    private void Update()
    {
        shootTimeCnt -= Time.deltaTime;
        if (shootTimeCnt  < 0.0f)
        {
            // Shoot
            shootTimeCnt = shootInterval;
            shootLeft--;
            StartCoroutine(StartShoot(transform.position.x));

            if (shootLeft <= 0)
            {
                DestroySelf(0.5f);
            }
        }
    }

    IEnumerator StartShoot(float position)
    {
        AudioManager.Instance.PlaySFX("lightningbolt", 0.2f);
        GameObject tmp = Instantiate(lightningStrikeEffect, new Vector2(transform.position.x, -4f), Quaternion.identity);
        tmp.transform.SetParent(transform.parent);

        yield return new WaitForSeconds(0.45f);

        CheckHitDamage();
    }

    private void CheckHitDamage()
    {
        // DEAL DAMAGE TO PLAYER
        if (Mathf.Abs(player.transform.position.x - transform.position.x) < 1.5f && player.transform.position.y < 2f)
        {
            player.StartJump();
            player.DealDamage(dmg + Random.Range(0, maxDmg + 1), transform);

            GameObject tmp = Instantiate(lightningSparkEffect, player.transform.position, Quaternion.identity);
            tmp.transform.SetParent(transform.parent);
        }

        // DEAL DAMAGE TO ENEMY
        List<EnemyControl> enemyList = gameMng.GetMonsterList();

        if (enemyList.Count > 0)
        {
            foreach (EnemyControl enemy in enemyList)
            {
                if (Mathf.Abs(enemy.transform.position.x - transform.position.x) < 1.5f + enemy.GetCollider().bounds.size.x / 2f
                        && enemy.transform.position.y < 2f && enemy.GetName() != "Paku")
                {
                    int calculatedDamage = dmg / 2 + Random.Range(0, maxDmg / 2 + 1);

                    enemy.DealDamage(calculatedDamage);
                    GameObject tmp = Instantiate(lightningSparkEffect, enemy.transform.position, Quaternion.identity);
                    tmp.transform.SetParent(enemy.transform.parent);

                    Vector2 randomize = new Vector2(Random.Range(-enemy.GetComponent<Collider2D>().bounds.size.x / 2f, enemy.GetComponent<Collider2D>().bounds.size.x / 2f), Random.Range(-0.5f, 0.5f));
                    Vector2 floatDirection = new Vector2(0.0f, 1.0f);
                    gameMng.SpawnFloatingText(new Vector2(enemy.transform.position.x, enemy.transform.position.y + enemy.GetComponent<Collider2D>().bounds.size.y * 0.75f) + randomize
                                                 , 2f + Random.Range(0.0f, 1.0f), 25f + Random.Range(0.0f, 25.0f),
                                                 calculatedDamage.ToString(), Color.white, floatDirection.normalized, 50f);
                }
            }
        }
    }
}

