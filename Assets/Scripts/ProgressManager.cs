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
        BreakFall,
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

    public List<Skill> NewStuffUnlocked(int currentLevel)
    {
        List<Skill> newUnlock = new List<Skill>();
        if (currentLevel > unlockLevel)
        {
            PlayerPrefs.SetInt("UnlockLevel", currentLevel);
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
                rtn.skill_name = "<color=#ff00ffff>Move Speed</color>";
                rtn.skill_description = "Increase your movement speed by <color=#ff00ffff>" + (rtn.value * 10).ToString() + "</color>.";
                rtn.skill_Icon = "movespeed";
                break;
            case Skill.BaseDamage:
                rtn.value = 4 + Random.Range(0, 2);
                rtn.skill_name = "<color=red>Base Damage</color>";
                rtn.skill_description = "Increase your base attack damage by <color=red>" + ((int)rtn.value).ToString() + "</color>.";
                rtn.skill_Icon = "basedamage";
                break;
            case Skill.MaxDamage:
                rtn.value = 4 + Random.Range(0, 3);
                rtn.skill_name = "<color=#800000ff>Max Damage</color>";
                rtn.skill_description = "Increase your extra random damage output on top of your normal damage by <color=#800000ff>" + ((int)rtn.value).ToString() + "</color>.";
                rtn.skill_Icon = "maxdamage";
                break;
            case Skill.Vitality:
                rtn.value = 13 + Random.Range(0, 10);
                rtn.skill_name = "<color=#00ff00ff>Vitality</color>";
                rtn.skill_description = "Increase your max health by <color=#00ff00ff>" + rtn.value.ToString() + "</color>.";
                rtn.skill_Icon = "vitality";
                break;
            case Skill.DashCooldown:
                rtn.value = 12 + Random.Range(0, 8);
                rtn.skill_name = "<color=#ffff00ff>Dash Cooldown</color>";
                rtn.skill_description = "Decrease dash cooldown time by <color=#ffff00ff>" + rtn.value.ToString() + "%</color>.";
                rtn.skill_Icon = "dashcooldown";
                break;
            case Skill.DashDamage:
                rtn.value = 11 + Random.Range(0, 4);
                rtn.skill_name = "<color=#008080ff>Dash Damage</color>";
                rtn.skill_description = "Dashing deal <color=#008080ff>" + rtn.value.ToString() + " damage</color> but cooldown <color=red>0.5 sec</color> longer. The number will stack.";
                rtn.skill_Icon = "dashdamage";
                break;
            case Skill.Stamina:
                rtn.value = 15 + Random.Range(0, 9);
                rtn.skill_name = "<color=blue>Max Stamina</color>";
                rtn.skill_description = "Give you <color=blue>" + rtn.value.ToString() + "</color> more stamina point to spent.";
                rtn.skill_Icon = "stamina";
                break;
            case Skill.StaminaRecoverSpeed:
                rtn.value = 1;
                rtn.skill_name = "<color=#00ff00ff>Stamina Recover</color>";
                rtn.skill_description = "Increase stamina regenerate per tick by <color=#00ff00ff>" + rtn.value + "</color>.";
                rtn.skill_Icon = "staminarecover";
                break;
            case Skill.HPRegen:
                rtn.value = 7 + Random.Range(0, 4);
                rtn.skill_name = "<color=#008000ff>HP Regen</color>";
                rtn.skill_description = "Heal <color=#008000ff>"+ rtn.value + "</color> hp when proceeding to next level.";
                rtn.skill_Icon = "hpregen";
                break;
            case Skill.LightningLash:
                rtn.value = 15 + Random.Range(0, 25);
                rtn.skill_name = "<color=#008000ff>Lightning Lash</color>";
                rtn.skill_description = "Dash-attack cost no stamina, deal double damage and heals <color=#008000ff>" + rtn.value + "%</color> of your damage dealt into HP.";
                rtn.skill_Icon = "lifesteal";
                break;
            case Skill.LifeDrain:
                rtn.value = 4 + Random.Range(0, 3);
                rtn.skill_name = "<color=#800000ff>Life Drain</color>";
                rtn.skill_description = "Heal <color=#800000ff>" + rtn.value + "</color> hp when you kill an enemy. The number will stack.";
                rtn.skill_Icon = "lifedrain";
                break;
            case Skill.Survivor:
                rtn.value = 1;
                rtn.skill_name = "<color=#ffff00ff>Survivor</color>";
                rtn.skill_description = "Give you a second chance when you're dead.";
                rtn.skill_Icon = "survive";
                break;
            case Skill.ComboMaster:
                rtn.value = 7 + Random.Range(0, 4);
                rtn.skill_name = "<color=blue>Combo Master</color>";
                rtn.skill_description = "Gain <color=blue>" + rtn.value.ToString() + "</color> stamina per combo when combo ended.";
                rtn.skill_Icon = "combomaster";
                break;
            case Skill.BreakFall:
                rtn.value = 50 + Random.Range(0, 30);
                rtn.skill_name = "<color=#008080ff>Break-Fall</color>";
                rtn.skill_description = "Crouching block an attack by the cost of <color=#008080ff>" + rtn.value + "</color> stamina.";
                rtn.skill_Icon = "breakfall";
                break;
            case Skill.Windrunner:
                rtn.value = 40 + Random.Range(0, 50);
                rtn.skill_name = "<color=#00ffffff>Windrunner</color>";
                rtn.skill_description = "Gain 60 movement speed and attack cost half stamina after dash for " + (float)rtn.value/100f + " second. The time do stack.";
                rtn.skill_Icon = "windrunner";
                break;
            case Skill.Potion:
                rtn.value = 10 + Random.Range(0, 5);
                rtn.skill_name = "<color=#00ffffff>Healing Salve</color>";
                rtn.skill_description = "Gain a consumables potion that recharge every level. Grants <color=#00ffffff>" + rtn.value + "</color> health.";
                rtn.skill_Icon = "potion";
                break;
            default:
                Debug.Log("<color=red>skill data not found!</color>");
                break;
        }
        return rtn;
    }
}
