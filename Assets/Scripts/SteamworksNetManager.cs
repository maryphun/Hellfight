using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if DISABLESTEAMWORKS
using Steamworks;
#endif
public class SteamworksNetManager : Singleton<SteamworksNetManager>
{
    public void UpdateLevelStat(int currentLevel)
    {
#if DISABLESTEAMWORKS
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
#endif
    }

    public void CheckLevelAchievement(int currentLevel)
    {
#if DISABLESTEAMWORKS
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
#endif
    }

    public void UnlockAchievement(string code)
    {
#if DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            // Check Achievement and set
            Steamworks.SteamUserStats.GetAchievement(code, out bool achievementCompleted);
            if (!achievementCompleted)
            {
                SteamUserStats.SetAchievement(code);
                SteamUserStats.StoreStats();
            }
        }
#endif
    }

    public void ClearSteamAchievement(string code)
    {
#if DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            SteamUserStats.ClearAchievement(code);
            SteamUserStats.StoreStats();
            Debug.Log("Steam Achievement reset: " + code);
        }
#endif
    }

    public void SteamLeaderboard()
    {
        if (SteamManager.Initialized)
        {
            //SteamUserStats.
        }
    }

    public bool SetSteamRichPresence(bool isPlaying, int level)
    {
#if DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            SteamFriends.SetRichPresence("steam_display", "#StatusWithScore");
            SteamFriends.SetRichPresence("score", level.ToString());
            SteamFriends.SetRichPresence("status", "fighting at level " + level.ToString());
            return true;
        }
#endif
        return false;
    }

    public string GetSteamID()
    {
#if DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            return SteamFriends.GetPersonaName();
        }
#endif
        return string.Empty;
    }

    public void SteamClouds()
    {
        if (SteamManager.Initialized)
        {
            
        }
    }

    public string GetSteamLanguage(bool convert)
    {
#if DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            string languageCode = SteamApps.GetCurrentGameLanguage();
            if (!convert)
            {
                return languageCode;
            }
            else 
            {
                switch (languageCode)
                {
                    case "english":
                        return "English";
                    case "japanese":
                        return "Japanese";
                    case "schinese":
                        return "ChineseSimplified";
                    case "tchinese":
                        return "ChineseTraditional";
                    default:
                        return languageCode;
                }
            }
        }
#endif
        return string.Empty;
    }

     public bool IsSteamConnected()
     {
#if DISABLE_STEAM
        return false;
#endif

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
        // unsupported platform.
        return false;
#endif
        if (SteamManager.Initialized)
        {
            return true;
        }
        return false;
     }
}

