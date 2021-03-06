using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OPS.AntiCheat.Prefs;
using Assets.SimpleLocalization;

public class ProgressManager : Singleton<ProgressManager>
{
    int unlockLevel;
   
    public int LoadUnlockLevel()
    {
        unlockLevel = ProtectedPlayerPrefs.GetInt("UnlockLevel", 0);
        return unlockLevel;
    }

    public int GetUnlockLevel()
    {
        return unlockLevel;
    }

    public List<Skill> NewStuffUnlocked(int currentLevel)
    {
        List<Skill> newUnlock = new List<Skill>();
        if (currentLevel > unlockLevel)
        {
            ProtectedPlayerPrefs.SetInt("UnlockLevel", currentLevel);
            int oldUnlockLevel = unlockLevel;
            unlockLevel = currentLevel;
            newUnlock = GetNewUnlock(oldUnlockLevel, unlockLevel);
        }
        return newUnlock;
    }

    private List<Skill> GetNewUnlock(int _old, int _new)
    {
        List<Skill> rtn = new List<Skill>();

        if (_old < 4 && _new >= 4)  // Lightning Lash
        {
            rtn.Add(Skill.LightningLash);
        }

        if (_old < 7 && _new >= 7)  // Recover
        {
            rtn.Add(Skill.Recover);
        }

        if (_old < 9 && _new >= 9)  // Combo Master
        {
            rtn.Add(Skill.ComboMaster);
        }

        if (_old < 10 && _new >= 10)  // BreakFall and potion
        {
            rtn.Add(Skill.BreakFall);
            rtn.Add(Skill.Potion);
        }

        if (_old < 18 && _new >= 18)  // Windrunner
        {
            rtn.Add(Skill.Windrunner);
        }

        if (_old < 12 && _new >= 12)  // Deflect
        {
            rtn.Add(Skill.Deflect);
        }

        if (_old < 13 && _new >= 13)  // Berserker
        {
            rtn.Add(Skill.Berserker);
        }

        return rtn;
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
                rtn.value = 5 + Random.Range(0, 4);
                rtn.skill_name = "<color=#800000ff>" + LocalizationManager.Localize("PowerupName.MaxDamage") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.MaxDamage", ((int)rtn.value).ToString());
                rtn.skill_Icon = "maxdamage";
                break;
            case Skill.Vitality:
                rtn.value = 13 + Random.Range(0, 11);
                rtn.skill_name = "<color=#00ff00ff>" + LocalizationManager.Localize("PowerupName.Vitality") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Vitality", ((int)rtn.value).ToString());
                rtn.skill_Icon = "vitality";
                break;
            case Skill.DashCooldown:
                rtn.value = 20 + Random.Range(0, 14);
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
                rtn.value = 10 + Random.Range(0, 6);
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
                rtn.value = 8 + Random.Range(0, 4);
                rtn.skill_name = "<color=blue>" + LocalizationManager.Localize("PowerupName.ComboMaster") + "</color>";
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
                rtn.value = 40 + Random.Range(0, 50);
                rtn.skill_name = "<color=#00ffffff>" + LocalizationManager.Localize("PowerupName.Windrunner") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Windrunner", ((int)rtn.value).ToString()); rtn.skill_Icon = "windrunner";
                break;
            case Skill.Potion:
                rtn.value = 20 + Random.Range(0, 5);
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
                rtn.value = 8 + Random.Range(0, 3);
                rtn.skill_name = "<color=#a52a2aff>" + LocalizationManager.Localize("PowerupName.Berserker") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Berserker", ((int)rtn.value).ToString());
                rtn.skill_Icon = "berserker";
                break;
            case Skill.Recover:
                rtn.value = 1;
                rtn.skill_name = "<color=#00ffffff>" + LocalizationManager.Localize("PowerupName.Recover") + "</color>";
                rtn.skill_description = LocalizationManager.Localize("PowerupDescription.Recover", ((int)rtn.value).ToString());
                rtn.skill_Icon = "recover";
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
