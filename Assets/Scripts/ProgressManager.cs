using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressManager : Singleton<ProgressManager>
{
    enum UnlockableContent
    {
        ComboMaster,
        Windrunner,
        Potion,
        DashAttack,

    }

    int unlockLevel;
   
    public int LoadUnlockLevel()
    {
        unlockLevel = PlayerPrefs.GetInt("UnlockLevel", 0);
        return unlockLevel;
    }

    public int GetUnlockLevel()
    {
        return unlockLevel;
    }

    public bool NewStuffUnlocked(int currentLevel)
    {
        if (currentLevel > unlockLevel)
        {
            PlayerPrefs.SetInt("UnlockLevel", currentLevel);
            int oldUnlockLevel = unlockLevel;
            unlockLevel = currentLevel;
            return true;
        }
        return false;
    }
}
