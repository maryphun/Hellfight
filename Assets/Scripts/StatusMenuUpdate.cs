using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatusMenuUpdate : MonoBehaviour
{
    [SerializeField] TMP_Text hp;
    [SerializeField] TMP_Text stamina;
    [SerializeField] TMP_Text movespeed;
    [SerializeField] TMP_Text atkdmg;
    [SerializeField] TMP_Text dashdmg;
    [SerializeField] TMP_Text critical;
    [SerializeField] TMP_Text lifesteal;
    [SerializeField] TMP_Text lifedrain;
    [SerializeField] TMP_Text dashcd;
    [SerializeField] TMP_Text resurrection;

    [SerializeField] Controller player;

    public void UpdateValue()
    {
        hp.SetText(player.GetMaxHP().ToString() + " + " + player.GetHPRegen().ToString() + "/s");
        stamina.SetText(player.GetMaxStamina().ToString() + " + " + player.GetStaminaRegen().ToString() + "/s");
        movespeed.SetText((player.GetMoveSpeed() * 10).ToString());
        atkdmg.SetText(player.GetAttackDamage().ToString() + " ~ " + (player.GetAttackDamage()+player.GetMaxDamage()).ToString());
        dashdmg.SetText(player.GetDashDamage().ToString());
        critical.SetText(player.GetCritical().ToString() + "%");
        lifesteal.SetText((player.GetLifesteal() * 100f).ToString() +"%");
        lifedrain.SetText(player.GetLifeDrain().ToString());
        dashcd.SetText(player.GetDashCD() + "sec");
        string tmp = player.GetSurvivor() ? "YES" : "NO";
        resurrection.SetText(tmp);
    }
}
