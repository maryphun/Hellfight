using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OPS.AntiCheat.Prefs;
using Assets.SimpleLocalization;

public struct UnlockData
{
    public string unlock_name;
    public string unlock_description;
    public string unlock_Icon;
}

public class ProgressManager : Singleton<ProgressManager>
{
    // SETTING
    const int c_progressionPointToNextLevelBase = 400;
    const int c_progressionPointToNextLevelPerLevel = 100;

    int progressPoint;       // gain 1000 point to unlock next level's reward.
    int unlockLevel;
    int oldUnlockLevel;

    List<KeyValuePair<int, Skill>> unlockList = new List<KeyValuePair<int, Skill>>();
    
    public void Initialization()
    {
        unlockList.Add(new KeyValuePair<int, Skill>(1, Skill.Potion));
        unlockList.Add(new KeyValuePair<int, Skill>(2, Skill.LightningLash));
        unlockList.Add(new KeyValuePair<int, Skill>(3, Skill.Battlecry));
        unlockList.Add(new KeyValuePair<int, Skill>(4, Skill.ComboMaster));
        unlockList.Add(new KeyValuePair<int, Skill>(5, Skill.BreakFall));
        unlockList.Add(new KeyValuePair<int, Skill>(6, Skill.Deflect));
        unlockList.Add(new KeyValuePair<int, Skill>(7, Skill.Windrunner));
        unlockList.Add(new KeyValuePair<int, Skill>(8, Skill.Berserker));
        unlockList.Add(new KeyValuePair<int, Skill>(9, Skill.Echo));
        unlockList.Add(new KeyValuePair<int, Skill>(10, Skill.Juggernaut));
    }

    public int LoadProgress()
    {
        unlockLevel = FBPP.GetInt("UnlockLevel", 0);
        oldUnlockLevel = unlockLevel;
        progressPoint = FBPP.GetInt("ProgressPoint", 0);

        Debug.Log("Unlock level = " + unlockLevel);
        return unlockLevel;
    }

    public void ResetProgress()
    {
        progressPoint = 0;
        unlockLevel = 0;
        oldUnlockLevel = 0;
    }

    public int GetUnlockLevel()
    {
        return unlockLevel;
    }
    public int GetProgressPoint()
    {
        return progressPoint;
    }

    public int GetPointRequiredToNextLevel()
    {
        return c_progressionPointToNextLevelBase + (unlockLevel * c_progressionPointToNextLevelPerLevel);
    }

    public void UpdateProgress(int point, int level, bool saveFileImmediately = true)
    {
        progressPoint = point;
        unlockLevel = level;
        FBPP.SetInt("ProgressPoint", progressPoint);
        FBPP.SetInt("UnlockLevel", unlockLevel);
        if (saveFileImmediately)
        {
            FBPP.Save();
        }
    }

    // return true if reach new record
    public bool UpdateHighestLevel(int level)
    {
        int oldRecord = FBPP.GetInt("HighestLevel", 0);
        if (oldRecord < level)
        {
            FBPP.SetInt("HighestLevel", level);
            FBPP.Save();
            return true;
        }
        return false;
    }

    public int GetHighestLevelRecord()
    {
        return FBPP.GetInt("HighestLevel", 0);
    }

    public List<UnlockData> GetNewUnlockList()
    {
        List<UnlockData> allUnlocks = new List<UnlockData>();

        // check skill unlock
        List<Skill> newSkillUnlock = new List<Skill>();
        newSkillUnlock = GetNewSkillUnlock(oldUnlockLevel, unlockLevel);
        oldUnlockLevel = unlockLevel;
        
        // convert skill unlock into data
        if (newSkillUnlock.Count > 0)
        {
            for (int i = 0; i < newSkillUnlock.Count; i++)
            {
                SelectionData skillData = EnumToData(newSkillUnlock[i]);
                UnlockData data;
                data.unlock_Icon = skillData.skill_Icon;
                data.unlock_name = skillData.skill_name;
                data.unlock_description = LocalizationManager.Localize("UnlockDescription." + newSkillUnlock[i].ToString());

                allUnlocks.Add(data);
            }
        }

        return allUnlocks;
    }

    private List<Skill> GetNewSkillUnlock(int _old, int _new)
    {
        List<Skill> rtn = new List<Skill>();
        Debug.Log("Old unlock level = " + _old.ToString() + ", New unlock level = " + _new.ToString());
        for (int i = 0; i < unlockList.Count; i++)
        {
            if (_old < unlockList[i].Key && _new >= unlockList[i].Key)
            {
                Debug.Log("Unlock skill " + unlockList[i].Value.ToString());
                rtn.Add(unlockList[i].Value);
            }
        }

        return rtn;
    }

    public bool IsSkillUnlocked(Skill skill)
    {
        for (int i = 0; i < unlockList.Count; i++)
        {
            if (unlockList[i].Value == skill)
            {
                if (unlockList[i].Key > unlockLevel)
                {
                    // not unlocked yet
                    return false;
                }
                else
                {
                    // unlocked
                    return true;
                }
            }
        }

        // the skill is not in unlock list ---- it's unlocked by default.
        return true;
    }

    public SelectionData EnumToData(Skill skill)
    {
        SelectionData rtn;
        rtn.value = 0;
        rtn.skill_name = string.Empty;
        rtn.skill_description = string.Empty;
        rtn.skill_Icon = string.Empty;

        switch (skill)
        {
            case Skill.MoveSpeed:
                rtn.value = 1 + Random.Range(0, 3);
                rtn.skill_name = "<color=#ff00ffff>"　+ LocalizationManager.Localize("PowerupName.MoveSpeed") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.MoveSpeed", (rtn.value * 10).ToString());
                rtn.skill_Icon = "movespeed";
                break;
            case Skill.BaseDamage:
                rtn.value = 3 + Random.Range(0, 2);
                rtn.skill_name = "<color=red>" + LocalizationManager.Localize("PowerupName.BaseDamage") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.BaseDamage", ((int)rtn.value).ToString());
                rtn.skill_Icon = "basedamage";
                break;
            case Skill.MaxDamage:
                rtn.value = 4 + Random.Range(0, 4);
                rtn.skill_name = "<color=#800000ff>" + LocalizationManager.Localize("PowerupName.MaxDamage") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.MaxDamage", ((int)rtn.value).ToString());
                rtn.skill_Icon = "maxdamage";
                break;
            case Skill.Vitality:
                rtn.value = 12 + Random.Range(0, 20);
                rtn.skill_name = "<color=#00ff00ff>" + LocalizationManager.Localize("PowerupName.Vitality") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Vitality", ((int)rtn.value).ToString());
                rtn.skill_Icon = "vitality";
                break;
            case Skill.DashCooldown:
                rtn.value = 18 + Random.Range(0, 16);
                rtn.skill_name = "<color=#ffff00ff>" + LocalizationManager.Localize("PowerupName.DashCooldown") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.DashCooldown", ((int)rtn.value).ToString());
                rtn.skill_Icon = "dashcooldown";
                break;
            case Skill.DashDamage:
                rtn.value = 11 + Random.Range(0, 4);
                rtn.skill_name = "<color=#008080ff>" + LocalizationManager.Localize("PowerupName.DashDamage") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.DashDamage", ((int)rtn.value).ToString());
                rtn.skill_Icon = "dashdamage";
                break;
            case Skill.Stamina:
                rtn.value = 15 + Random.Range(0, 9);
                rtn.skill_name = "<color=blue>" + LocalizationManager.Localize("PowerupName.MaxStamina") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.MaxStamina", ((int)rtn.value).ToString());
                rtn.skill_Icon = "stamina";
                break;
            case Skill.StaminaRecoverSpeed:
                rtn.value = 1;
                rtn.skill_name = "<color=#00ff00ff>" + LocalizationManager.Localize("PowerupName.StaminaRecoverSpeed") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.StaminaRecoverSpeed", ((int)rtn.value).ToString());
                rtn.skill_Icon = "staminarecover";
                break;
            case Skill.HPRegen:
                rtn.value = 10 + Random.Range(0, 7);
                rtn.skill_name = "<color=#ff2222ff>" + LocalizationManager.Localize("PowerupName.HPRegen") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.HPRegen", ((int)rtn.value).ToString());
                rtn.skill_Icon = "hpregen";
                break;
            case Skill.LightningLash:
                rtn.value = 10 + Random.Range(0, 9);
                rtn.skill_name = "<color=#800080ff>" + LocalizationManager.Localize("PowerupName.LightningLash") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.LightningLash", ((int)rtn.value).ToString());
                rtn.skill_Icon = "lightninglash";
                break;
            case Skill.LifeDrain:
                rtn.value = 4 + Random.Range(0, 3);
                rtn.skill_name = "<color=#800000ff>" + LocalizationManager.Localize("PowerupName.LifeDrain") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.LifeDrain", ((int)rtn.value).ToString());
                rtn.skill_Icon = "lifedrain";
                break;
            case Skill.Survivor:
                rtn.value = 1;
                rtn.skill_name = "<color=#ffff00ff>" + LocalizationManager.Localize("PowerupName.Survivor") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Survivor", ((int)rtn.value).ToString());
                rtn.skill_Icon = "survive";
                break;
            case Skill.ComboMaster:
                rtn.value = 7 + Random.Range(0, 4);
                rtn.skill_name = "<color=#afe1afff>" + LocalizationManager.Localize("PowerupName.ComboMaster") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.ComboMaster", ((int)rtn.value).ToString());
                rtn.skill_Icon = "combomaster";
                break;
            case Skill.BreakFall:
                rtn.value = 30 + Random.Range(0, 10);
                rtn.skill_name = "<color=#008080ff>" + LocalizationManager.Localize("PowerupName.Break-Fall") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Break-Fall", ((int)rtn.value).ToString());
                rtn.skill_Icon = "breakfall";
                break;
            case Skill.Windrunner:
                rtn.value = 1 + Random.Range(0, 3);
                rtn.skill_name = "<color=#00ffffff>" + LocalizationManager.Localize("PowerupName.Windrunner") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Windrunner", ((int)rtn.value).ToString()); rtn.skill_Icon = "windrunner";
                break;
            case Skill.Potion:
                rtn.value = 15 + Random.Range(0, 11);
                rtn.skill_name = "<color=#00ffffff>" + LocalizationManager.Localize("PowerupName.HealingSalve") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.HealingSalve", ((int)rtn.value).ToString());
                rtn.skill_Icon = "potion";
                break;
            case Skill.Deflect:
                rtn.value = 1;
                rtn.skill_name = "<color=#800080ff>" + LocalizationManager.Localize("PowerupName.Deflect") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Deflect", ((int)rtn.value).ToString());
                rtn.skill_Icon = "deflect";
                break;
            case Skill.Berserker:
                rtn.value = 1 + Random.Range(0, 3);
                rtn.skill_name = "<color=#a52a2aff>" + LocalizationManager.Localize("PowerupName.Berserker") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Berserker", ((int)rtn.value).ToString());
                rtn.skill_Icon = "berserker";
                break;
            case Skill.Battlecry:
                rtn.value = 50;
                rtn.skill_name = "<color=#00ffffff>" + LocalizationManager.Localize("PowerupName.Battlecry") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Battlecry", ((int)rtn.value).ToString());
                rtn.skill_Icon = "recover";
                break;
            case Skill.Echo:
                rtn.value = 10 + Random.Range(0, 22);
                rtn.skill_name = "<color=#ffa500ff>" + LocalizationManager.Localize("PowerupName.Echo") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Echo", ((int)rtn.value).ToString());
                rtn.skill_Icon = "echo";
                break;
            case Skill.Juggernaut:
                rtn.value = 8 + Random.Range(0, 8);
                rtn.skill_name = "<color=#4c00b0ff>" + LocalizationManager.Localize("PowerupName.Juggernaut") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Juggernaut", ((int)rtn.value).ToString());
                rtn.skill_Icon = "juggernaut";
                break;
            default:
                Debug.Log("<color=red>skill data not found!</color>");
                break;
        }

        // localization font
        if (LocalizationManagerHellFight.Instance().GetCurrentLanguage() == "Japanese")
        {
            rtn.skill_name = "<font=JPPixel SDF>" + rtn.skill_name + "</font>";
            rtn.skill_description = "<font=JPPixel SDF>" + rtn.skill_description + "</font>";
        }
        return rtn;
    }

}
