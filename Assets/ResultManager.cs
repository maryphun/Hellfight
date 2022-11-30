using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Result
{
    public Result()
    {
        monsterSlayed = 0;
        bossSlayed = 0;
        levelReached = 0;
        totalTime = 0;
    }

    public int GetAverageTimePerLevel()
    {
        return totalTime / levelReached;
    }

    public int monsterSlayed;
    public int bossSlayed;
    public int levelReached;
    public int totalTime;
}

// Class that use to calculate the result of this run. Reset everytime a run start!!
public class ResultManager : Singleton<ResultManager>
{
    Result m_result;
    public void GameReset()
    {
        m_result = new Result();
    }

    public void CountMonsterKill()
    {
        m_result.monsterSlayed++;
    }

    public void CountBossKill()
    {
        m_result.bossSlayed++;
    }

    public void SetLevelReached(int level)
    {
        m_result.levelReached = level;
    }

    public void SetTotalTime(int second)
    {
        m_result.totalTime = second;
    }

    public Result GetResult()
    {
        return m_result;
    }
}
