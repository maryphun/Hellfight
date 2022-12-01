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
    LifeDrain,
    Survivor,
    ComboMaster,
    Windrunner,
    Potion,
    DashAttack,
    BreakFall,
    LightningLash,
    Deflect,
    Berserker,
    Battlecry,
    Echo,
    Juggernaut,

    List_Number
}

public struct SelectionData
{
    public string skill_name;
    public string skill_description;
    public string skill_Icon;
    public float value;
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
    
    int selecting = -1;
    RectTransform rect;
    GameManager gameMng;
    bool choiceMade;
    Controller player;
    int lastKeyMap = 0;
    [SerializeField] Selection[] selection = new Selection[3];
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text description;
    public PlayerAction _input;

    // INPUT SYSTEM
    private void Awake()
    {
        _input = new PlayerAction();
        _input.PlayerControls.Move.performed += ctx => SelectionChange(new Vector2(Mathf.RoundToInt(ctx.ReadValue<float>()), 0));
        _input.MenuControls.Move.performed += ctx => SelectionChange(new Vector2(0, Mathf.RoundToInt(ctx.ReadValue<float>())));
        _input.MenuControls.Confirm.performed += ctx => Select(selecting);
    }

    private void OnEnable()
    {
        _input.Enable();
    }

    private void OnDisable()
    {
       _input.Disable();
    }

    private void SelectionChange(Vector2 direction)
    {
        if (choiceMade) return;
        int oldSelecting = selecting;

        // KEYBOARD CONTROL
        if (direction.x < 0)
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

        if (direction.x > 0)
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

        if (direction.y != 0)
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
        int unlockLevel = ProgressManager.Instance().GetUnlockLevel();
        List<Skill> possibleSkill = new List<Skill>();

        // Add Possible Skill
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
            if (player.GetStaminaRegen() <= level * 2)
            {
                CheckAndAdd(possibleSkill, Skill.StaminaRecoverSpeed);
            }
            if (!gameMng.IsPotionSelected())
            {
                CheckAndAdd(possibleSkill, Skill.Potion);
            }
        }
        if (level >= 3)
        {
            if (player.GetDashDamage() < 100)
            {
                CheckAndAdd(possibleSkill, Skill.DashDamage);
            }
            if (player.GetDashCD() > 0.05f)
            {
                CheckAndAdd(possibleSkill, Skill.DashCooldown);
            }
            CheckAndAdd(possibleSkill, Skill.HPRegen);
            CheckAndAdd(possibleSkill, Skill.LifeDrain);
        }

        if (level >= 5)
        {
            if (!player.GetDeflect())
            {
                CheckAndAdd(possibleSkill, Skill.Deflect);
            }
            if (player.GetLightningLash() < 0.15f)
            {
                CheckAndAdd(possibleSkill, Skill.LightningLash);
            }
            if (!gameMng.IsSurvivorSelected() && !player.GetSurvivor())
            {
                possibleSkill.Add(Skill.Survivor);
            }
            CheckAndAdd(possibleSkill, Skill.Echo);
        }
        if (level >= 7)
        {
            if (!player.GetIsBattlecry())
            {
                CheckAndAdd(possibleSkill, Skill.Battlecry);
            }
            if (player.GetJuggernaut() == 0)
            {
                CheckAndAdd(possibleSkill, Skill.Juggernaut);
            }
        }
        if (level >= 8)
        {
            CheckAndAdd(possibleSkill, Skill.ComboMaster);
            CheckAndAdd(possibleSkill, Skill.Berserker);
        }
        if (level >= 13 && player.GetBreakFallCost() == 0f)
        {
            CheckAndAdd(possibleSkill, Skill.BreakFall);
        }
        if (level >= 10 && player.GetWindrunner() < 1.5f)
        {
            CheckAndAdd(possibleSkill, Skill.Windrunner);
        }

        for (int i = 0; i < 3; i++)
        {
            // take random skill on the list
            Skill randomedSkill = possibleSkill[Random.Range(0, possibleSkill.Count)];

            // record
            selection[i].data = ProgressManager.Instance().EnumToData(randomedSkill);

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

    public void MouseSelected()
    {
        Debug.Log("is mouse input");
        for (int i = 0; i < 3; i++)
        {
            selection[i].skill_Icon.color = new Color(1f, 1f, 1f, 1.0f);
            selection[i].skill_name.color = new Color(1f, 1f, 1f, 1.0f);
            selection[i].skill_description.color = new Color(1f, 1f, 1f, 1.0f);
        }
    }

    public void Select(int index)
    {
        if (index == -1) return;
        if (choiceMade) return;

        choiceMade = true;

        AudioManager.Instance.PlaySFX("powerup");

        StartCoroutine(SelectionAnimation(index));

        // apply the effect
        player.ApplyBonus(selection[index].skillEnum, selection[index].data.value);

        // Bonus that only come once
        if (selection[index].skillEnum == Skill.Survivor)
        {
            gameMng.Survivorselected();
        }
        if (selection[index].skillEnum == Skill.Potion)
        {
            gameMng.PotionSelected();
        }
    }

    private bool CheckAndAdd(List<Skill> list, Skill targetSkill)
    {
        // check if skill is unlocked
        if (!ProgressManager.Instance().IsSkillUnlocked(targetSkill))
            return false;

        // check if this skill is spawned last level
        if (gameMng.IsSkillSpawnedLastLevel(targetSkill))
            return false;
        
        // add this skill into the potential choice list
        list.Add(targetSkill);
        return true;
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
}
