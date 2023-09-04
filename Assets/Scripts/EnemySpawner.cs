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

    [Header("Setting")]
    [SerializeField] GameManager gameMng;
    [SerializeField] Transform world;
    [SerializeField] LevelInfo[] levelInfo;
    [SerializeField] RandomInfo[] randomList;
    [SerializeField] List<Coroutine> spawningList = new List<Coroutine>();

    // �����_���œG�𐶐�����ꍇ�̐ݒ�
    [Header("Random Setting")]
    [SerializeField] int randomSpawnCountMin = 2;
    [SerializeField] int randomSpawnCountMax = 4;
    [SerializeField] float randomSpawnTimeMin = 1.5f;
    [SerializeField] float randomSpawnTimeMax = 2.5f;

    /// <summary>
    /// �G�����J�n
    /// </summary>
    public int StartSpawning(int level)
    {
        if (level > levelInfo.Length || !levelInfo[level - 1].activate) // �ݒ肳��Ă��Ȃ����x���̓����_���Ń����X�^�[�𐶐�����
        {
            int totalCount = Random.Range(randomSpawnCountMin, randomSpawnCountMax); // �G��������
            for (int i = 0; i < totalCount; i++)
            {
                // �����I�Ȕ͈͓��ŁA���݃��x���ŋ�������L�������o���Ȃ��悤�ɂ���
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

                float time = Random.Range(randomSpawnTimeMin, i * randomSpawnTimeMax);
                if (i > (totalCount - 1) /2)
                {
                    // �L��������C�ɐ��������Ɠ������̂ŁA�������ԂɊԂ��󂯂�
                    time *= randomSpawnTimeMin;
                }
                
                // �ݒ芮��
                spawningList.Add(StartCoroutine(SpawnMonster(prefab, SpawnDirection.RANDOM, time, level)));
            }

            return totalCount;
        }

        
        foreach (SpawnInfo spwnInfo in levelInfo[level - 1].spawnInfo) // ���x���f�U�C���f�[�^����Ȃ炻������������ēG�L�����𐶐�����
        {
            spawningList.Add(StartCoroutine(SpawnMonster(spwnInfo.monster, spwnInfo.direction, spwnInfo.timing, level)));
        }

        return levelInfo[level - 1].spawnInfo.Length;
    }

    /// <summary>
    /// ���x��������
    /// </summary>
    public void ResetSpawner()
    {
        foreach(Coroutine spawner in spawningList)
        {
            StopCoroutine(spawner);
        }
        spawningList.Clear();
    }

    /// <summary>
    /// �G�L��������
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnMonster(GameObject monsterPrefab, SpawnDirection direction, float timing, int level)
    {
        yield return new WaitForSeconds(timing);

        EnemyControl tmp = Instantiate(monsterPrefab, DirectionToVector(direction), Quaternion.identity).GetComponent<EnemyControl>();
        tmp.transform.SetParent(world, true);
        tmp.GetGraphic().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        gameMng.RegisterMonsterInList(tmp);
        if (tmp.IsBoss()) gameMng.RregisterBoss(tmp);

        tmp.SetLevel(level);

        if (direction == SpawnDirection.TOP_LEFT || direction == SpawnDirection.TOP_MIDDLE || direction == SpawnDirection.TOP_RIGHT)
        {
            tmp.SuperLand();
        }
    }

    /// <summary>
    /// �w������ɑ΂��ēG�̏����ʒu��ݒ�
    /// </summary>
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
