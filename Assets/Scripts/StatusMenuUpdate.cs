using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatusMenuUpdate : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] TMP_Text hp;
    [SerializeField] TMP_Text stamina;
    [SerializeField] TMP_Text movespeed;
    [SerializeField] TMP_Text atkdmg;
    [SerializeField] TMP_Text dashdmg;
    [SerializeField] TMP_Text windrunner;
    [SerializeField] TMP_Text lightningDash;
    [SerializeField] TMP_Text lifedrain;
    [SerializeField] TMP_Text dashcd;
    [SerializeField] TMP_Text resurrection;
    [SerializeField] TMP_Text breakfall;
    [SerializeField] TMP_Text timer;

    [SerializeField] Controller player;

    /// <summary>
    /// ステータスメニューを更新
    /// </summary>
    /// <param name="time"></param>
    public void UpdateValue(int time)
    {
        hp.SetText(player.GetMaxHP().ToString() + " + " + player.GetHPRegen().ToString());
        stamina.SetText(player.GetMaxStamina().ToString() + " + " + player.GetStaminaRegen().ToString() + "/s");
        movespeed.SetText((player.GetMoveSpeed() * 10).ToString());
        atkdmg.SetText(player.GetAttackDamage().ToString() + " ~ " + (player.GetAttackDamage()+player.GetMaxDamage()).ToString());
        dashdmg.SetText(player.GetDashDamage().ToString());
        windrunner.SetText(player.GetWindrunner().ToString() + " sec");
        lightningDash.SetText(player.GetLightningLash().ToString("p"));
        lifedrain.SetText(player.GetLifeDrain().ToString());
        dashcd.SetText(player.GetDashCD().ToString("F2") + " sec");
        breakfall.SetText(player.GetBreakFallCost().ToString());
        string tmp = player.GetSurvivor() ? "YES" : "NO";
        resurrection.SetText(tmp);
        timer.SetText((time / 60).ToString() + "m" + (time % 60).ToString() + "s");
    }
}
