using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

[System.Serializable]
public enum Skill
{
    MoveSpeed,
    BaseDamage,
    MaxDamage,
    Vitality,
    DashCooldown,
    DashDamage,
    Stamina,
    StaminaRecoverSpeed,
    HPRegen,
    Lifesteal,
    LifeDrain,
    Survivor,
    ComboMaster,

    List_Number
}

public class AbilityLearnPanel : MonoBehaviour
{
    [System.Serializable]
    struct Selection
    {
        [SerializeField] public TMP_Text skill_name;
        [SerializeField] public TMP_Text skill_description;
        [SerializeField] public Image skill_Icon;
        public SelectionData data;
        public Skill skillEnum;
    }

    struct SelectionData
    {
        public string skill_name;
        public string skill_description;
        public string skill_Icon;
        public float value;
    }

    int selecting = -1;
    RectTransform rect;
    GameManager gameMng;
    bool choiceMade;
    Controller player;
    [SerializeField] Selection[] selection = new Selection[3];
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text description;

    private void Start()
    {
        //img1 = Resources.Load<Sprite>("Icon/");
    }

    private void Update()
    {
        if (choiceMade) return;
        int oldSelecting = selecting;
        // KEYBOARD CONTROL
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (selecting == -1)
            {
                selecting = 0;
            }
            else
            {
                selecting = selecting - 1;
                if (selecting < 0)
                {
                    selecting = 2;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (selecting == -1)
            {
                selecting = 2;
            }
            else
            {
                selecting = selecting + 1;
                if (selecting > 2)
                {
                    selecting = 0;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (selecting == -1)
            {
                selecting = 1;
            }
        }

        if (oldSelecting != selecting)
        {
            SelectChange();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z))
        {
            if (selecting != -1)
            {
                Select(selecting);
                return;
            }
        }
    }

    private void SelectChange()
    {
        AudioManager.Instance.PlaySFX("menuOpen", 0.5f);
        for (int i = 0; i < 3; i++)
        {
            if (selecting == i)
            {
                selection[i].skill_Icon.color = new Color(1f, 1f, 1f, 1.0f);
                selection[i].skill_name.color = new Color(1f, 1f, 1f, 1.0f);
                selection[i].skill_description.color = new Color(1f, 1f, 1f, 1.0f);
            }
            else
            {
                selection[i].skill_Icon.color = new Color(0.25f, 0.25f, 0.25f, 1.0f);
                selection[i].skill_name.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                selection[i].skill_description.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
    }

    public void Selecting(int index)
    {
        if (choiceMade) return;
        if (index == selecting) return;
        selecting = index;
        SelectChange();
    }

    public void Initialize(int level, GameManager mng, Controller plyer)
    {
        gameObject.SetActive(true);

        gameMng = mng;
        player = plyer;
        choiceMade = false;
        rect = GetComponent<RectTransform>();
        AudioManager.Instance.PlaySFX("abilityPanel");
        rect.DOMoveY(-333.33f, 0.0f);
        rect.DOMoveY(0.0f, 1f);
        selecting = -1;

        for (int i = 0; i < 3; i++)
        {
            // reset transparent
            selection[i].skill_Icon.color = Color.white;
            selection[i].skill_description.color = new Color(selection[i].skill_description.color.r, 
                selection[i].skill_description.color.g, selection[i].skill_description.color.b, 1.0f);
            selection[i].skill_name.color = Color.white;
        }

        InitializeSelection(level);
    }

    private void InitializeSelection(int level)
    {
        List<Skill> possibleSkill = new List<Skill>();

        if (level >= 1)
        {
            CheckAndAdd(possibleSkill, Skill.MoveSpeed);
            if (player.GetAttackDamage() < level + 10 || level < 3)
            {
                CheckAndAdd(possibleSkill, Skill.BaseDamage);
            }
            CheckAndAdd(possibleSkill, Skill.MaxDamage);
            CheckAndAdd(possibleSkill, Skill.Vitality);
            CheckAndAdd(possibleSkill, Skill.Stamina);
            CheckAndAdd(possibleSkill, Skill.StaminaRecoverSpeed);
        }
        if (level >= 3)
        {
            if (player.GetDashDamage() < 30)
            {
                CheckAndAdd(possibleSkill, Skill.DashDamage);
            }
            CheckAndAdd(possibleSkill, Skill.DashCooldown);
            CheckAndAdd(possibleSkill, Skill.HPRegen);

            if (player.GetLifesteal() < 0.15f)
            {
                CheckAndAdd(possibleSkill, Skill.Lifesteal);
            }
            CheckAndAdd(possibleSkill, Skill.LifeDrain);
        }
        if (level >= 5 && !gameMng.IsSurvivorSelected())
        {
            possibleSkill.Add(Skill.Survivor);
        }
        if (level >= 9)
        {
            CheckAndAdd(possibleSkill, Skill.ComboMaster);
        }

        for (int i = 0; i < 3; i++)
        {
            // take random skill on the list
            Skill randomedSkill = possibleSkill[Random.Range(0, possibleSkill.Count)];

            // record
            selection[i].data = EnumToData(randomedSkill);

            // remove the picked skill from the list
            possibleSkill.Remove(randomedSkill);

            // apply
            selection[i].skill_name.SetText(selection[i].data.skill_name);
            selection[i].skill_description.SetText(selection[i].data.skill_description);
            selection[i].skill_Icon.sprite = Resources.Load<Sprite>("Icon/" + selection[i].data.skill_Icon);
            selection[i].skillEnum = randomedSkill;

            // record this skill into game manager so next time it wont spawn the same thing again
            gameMng.SkillSpawnedRecord(selection[i].skillEnum, i);
        }
    }

    public void Select(int index)
    {
        if (choiceMade) return;

        choiceMade = true;

        AudioManager.Instance.PlaySFX("powerup");

        StartCoroutine(SelectionAnimation(index));

        // apply the effect
        player.ApplyBonus(selection[index].skillEnum, selection[index].data.value);

        if (selection[index].skillEnum == Skill.Survivor)
        {
            gameMng.Survivorselected();
        }
    }

    private bool CheckAndAdd(List<Skill> list, Skill targetSkill)
    {
        if (!gameMng.IsSkillSpawnedLastLevel(targetSkill))
        {
            list.Add(targetSkill);
            return true;
        }
        return false;
    }

    IEnumerator SelectionAnimation(int selectedIndex)
    {
        // fade out title
        title.DOFade(0.0f, 0.5f);
        description.DOFade(0.0f, 0.5f);

        // choice have been made
        for (int i = 0; i < 3; i++)
        {
            if (i == selectedIndex)
            {
                selection[i].skill_Icon.rectTransform.DOMoveX(0f, 0.5f, false);
                selection[i].skill_description.rectTransform.DOMoveX(0f, 0.5f, false);
                selection[i].skill_name.rectTransform.DOMoveX(0f, 0.5f, false);
            }
            else
            {
                selection[i].skill_Icon.DOFade(0.0f, 0.5f);
                selection[i].skill_description.DOFade(0.0f, 0.5f);
                selection[i].skill_name.DOFade(0.0f, 0.5f);
            }
        }

        yield return new WaitForSeconds(1.5f);

        rect.DOMoveY(10f, 1f);

        yield return new WaitForSeconds(1f);

        gameMng.StartLevelCinematic();
        gameMng.NextLevel();
        Destroy(gameObject);
    }

    SelectionData EnumToData(Skill skill)
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
                rtn.skill_description = "Increase your movement speed by <color=#ff00ffff>" + (rtn.value*10).ToString() + "</color>.";
                rtn.skill_Icon = "movespeed";
                break;
            case Skill.BaseDamage:
                rtn.value = 2 + Random.Range(0, 5);
                rtn.skill_name = "<color=red>Base Damage</color>";
                rtn.skill_description = "Increase your base attack damage by <color=red>" + ((int)rtn.value).ToString() + "</color>.";
                rtn.skill_Icon = "basedamage";
                break;
            case Skill.MaxDamage:
                rtn.value = 1 + Random.Range(0, 8);
                rtn.skill_name = "<color=#800000ff>Max Damage</color>";
                rtn.skill_description = "Increase your extra random damage output on top of your normal damage by <color=#800000ff>" + ((int)rtn.value).ToString() + "</color>.";
                rtn.skill_Icon = "maxdamage";
                break;
            case Skill.Vitality:
                rtn.value = 8 + Random.Range(0, 15);
                rtn.skill_name = "<color=#00ff00ff>Vitality</color>";
                rtn.skill_description = "Increase your max health by <color=#00ff00ff>" + rtn.value.ToString() + "</color>.";
                rtn.skill_Icon = "vitality";
                break;
            case Skill.DashCooldown:
                rtn.value = 7 + Random.Range(0, 14);
                rtn.skill_name = "<color=#ffff00ff>Dash Cooldown</color>";
                rtn.skill_description = "Decrease dash cooldown time by <color=#ffff00ff>" + rtn.value.ToString() + "%</color>.";
                rtn.skill_Icon = "dashcooldown";
                break;
            case Skill.DashDamage:
                rtn.value = 1 + Random.Range(0, 15);
                rtn.skill_name = "<color=#008080ff>Dash Damage</color>";
                rtn.skill_description = "Dashing deal <color=#008080ff>" + rtn.value.ToString() + " damage</color> but cooldown <color=red>0.5 sec</color> longer. The effect will stack.";
                rtn.skill_Icon = "dashdamage";
                break;
            case Skill.Stamina:
                rtn.value = 10 + Random.Range(0, 31);
                rtn.skill_name = "<color=blue>Max Stamina</color>";
                rtn.skill_description = "Give you <color=blue>" + rtn.value.ToString() + "</color> more stamina point to spent.";
                rtn.skill_Icon = "stamina";
                break;
            case Skill.StaminaRecoverSpeed:
                rtn.value = 1 + Random.Range(0, 2);
                rtn.skill_name = "<color=#00ff00ff>Stamina Recover</color>";
                rtn.skill_description = "Increase stamina regenerate rate by <color=#00ff00ff>" + ((int)(((float)1)/((float)player.GetStaminaRegen()) * 100)).ToString() + "%</color>.";
                rtn.skill_Icon = "staminarecover";
                break;
            case Skill.HPRegen:
                rtn.value = 1;
                rtn.skill_name = "<color=#008000ff>HP Regen</color>";
                rtn.skill_description = "Heal <color=#008000ff>1</color> hp per seconds when you're standing still. The effect will stack.";
                rtn.skill_Icon = "hpregen";
                break;
            case Skill.Lifesteal:
                rtn.value = 1 + Random.Range(0, 6);
                rtn.skill_name = "<color=#008000ff>Lifesteal</color>";
                rtn.skill_description = "Allow you to gain <color=#008000ff>" + rtn.value + "%</color> of your attack damage into HP when damaging an enemy.";
                rtn.skill_Icon = "lifesteal";
                break;
            case Skill.LifeDrain:
                rtn.value = 1 + Random.Range(0, 5);
                rtn.skill_name = "<color=#800000ff>Life Drain</color>";
                rtn.skill_description = "Heal <color=#800000ff>" + rtn.value + "</color> hp when you kill an enemy. The effect will stack.";
                rtn.skill_Icon = "lifedrain";
                break;
            case Skill.Survivor:
                rtn.value = 1 + Random.Range(0, 10);
                rtn.skill_name = "<color=#ffff00ff>Survivor</color>";
                rtn.skill_description = "Give you a second chance when you're dead.";
                rtn.skill_Icon = "survive";
                break;
            case Skill.ComboMaster:
                rtn.value = 5 + Random.Range(0, 16);
                rtn.skill_name = "<color=blue>Combo Master</color>";
                rtn.skill_description = "Gain " + rtn.value.ToString() + " stamina per combo when combo ended.";
                rtn.skill_Icon = "combomaster";
                break;
            default:
                Debug.Log("<color=red>skill data not found!</color>");
                break;
        }
        return rtn;
    }
}
