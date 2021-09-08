using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnInfo
    {
        public GameObject monster;
        public float timing;
        public SpawnDirection direction;
    }

    [System.Serializable]
    public struct RandomInfo
    {
        public GameObject monster;
        public int availableLevel;
    }

    [System.Serializable]
    public struct LevelInfo
    {
        public bool activate;
        public SpawnInfo[] spawnInfo;
    }

    public enum SpawnDirection
    {
        TOP_LEFT,
        TOP_RIGHT,
        TOP_MIDDLE,
        LEFT,
        RIGHT,
        RANDOM,
        RANDOM_LEFT_RIGHT,
    }


    [SerializeField] GameManager gameMng;
    [SerializeField] Transform world;
    [SerializeField] LevelInfo[] levelInfo;
    [SerializeField] RandomInfo[] randomList;
    [SerializeField] List<Coroutine> spawningList = new List<Coroutine>();


    public int StartSpawning(int level)
    {
        if (level > levelInfo.Length || !levelInfo[level - 1].activate)
        {
            // random spawn
            int number = Random.Range(2, 4);
            for (int i = 0; i < number; i++)
            {
                bool randomed = false;
                GameObject prefab = randomList[0].monster;
                while (!randomed)
                {
                    int index = Random.Range(0, randomList.Length);
                    if (randomList[index].availableLevel < level)
                    {
                        prefab = randomList[index].monster;
                        randomed = true;
                    }
                }

                float time = Random.Range(1.5f, i * 2.5f);
                if (i > (number-1) /2)
                {
                    time *= 1.5f;
                }
                
                spawningList.Add(StartCoroutine(SpawnMonster(prefab, SpawnDirection.RANDOM, time, level)));
            }

            return number;
        }

        foreach (SpawnInfo spwnInfo in levelInfo[level-1].spawnInfo)
        {
            spawningList.Add(StartCoroutine(SpawnMonster(spwnInfo.monster, spwnInfo.direction, spwnInfo.timing, level)));
        }

        return levelInfo[level - 1].spawnInfo.Length;
    }

    public void ResetSpawner()
    {
        foreach( Coroutine spawner in spawningList)
        {
            StopCoroutine(spawner);
        }
        spawningList.Clear();
    }

    IEnumerator SpawnMonster(GameObject monsterPrefab, SpawnDirection direction, float timing, int level)
    {
        yield return new WaitForSeconds(timing);

        EnemyControl tmp = Instantiate(monsterPrefab, DirectionToVector(direction), Quaternion.identity).GetComponent<EnemyControl>();
        tmp.transform.SetParent(world, true);
        tmp.GetGraphic().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        gameMng.RegisterMonsterInList(tmp);
        tmp.SetLevel(level);

        if (direction == SpawnDirection.TOP_LEFT || direction == SpawnDirection.TOP_MIDDLE || direction == SpawnDirection.TOP_RIGHT)
        {
            tmp.SuperLand();
        }
    }

    Vector2 DirectionToVector(SpawnDirection direction)
    {
        Vector2 rtn;
        switch (direction)
        {
            case SpawnDirection.TOP_LEFT:
                rtn = new Vector2(-6f, 7f);
                break;
            case SpawnDirection.TOP_RIGHT:
                rtn = new Vector2(6f, 7f);
                break;
            case SpawnDirection.TOP_MIDDLE:
                rtn = new Vector2(-0f, 7f);
                break;
            case SpawnDirection.LEFT:
                rtn = new Vector2(-12f, 0f);
                break;
            case SpawnDirection.RIGHT:
                rtn = new Vector2(12f, 0f);
                break;
            case SpawnDirection.RANDOM_LEFT_RIGHT:
                rtn = DirectionToVector((SpawnDirection)Random.Range((int)SpawnDirection.LEFT, (int)SpawnDirection.RIGHT+1));
                break;
            default:
                rtn = DirectionToVector((SpawnDirection)Random.Range(0, 5));
                break;
        }

        return rtn;
    }
}
