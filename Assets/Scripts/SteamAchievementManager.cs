using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class SteamAchievementManager : Singleton<SteamAchievementManager>
{
    public void UpdateLevelStat(int currentLevel)
    {
        if (SteamManager.Initialized)
        {
            SteamUserStats.SetStat("CURRENTLEVEL", currentLevel);

            SteamUserStats.GetStat("MAXLEVEL", out int maxLevel);
            if (currentLevel > maxLevel)
            {
                SteamUserStats.SetStat("MAXLEVEL", currentLevel);
            }
            SteamUserStats.StoreStats();
        }
    }

    public void CheckLevelAchievement(int currentLevel)
    {
        if (SteamManager.Initialized)
        {
            // Update level stats
            SteamUserStats.GetStat("MAXLEVEL", out int maxLevel);

            // Check Achievement and set
            for (int i = 0; i < currentLevel; i++)
            {
                string achievementName = "LEVEL_" + i.ToString();
                Steamworks.SteamUserStats.GetAchievement(achievementName, out bool achievementCompleted);
                if (!achievementCompleted)
                {
                    SteamUserStats.SetAchievement(achievementName);
                    SteamUserStats.StoreStats();
                }
            }
        }
    }

    public void SteamLeaderboard()
    {
        if (SteamManager.Initialized)
        {
            //SteamUserStats.leader

        }
    }
}
