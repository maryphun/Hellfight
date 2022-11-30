using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashChargeBar : MonoBehaviour
{
    [SerializeField] List<DashChargeSlot> DashChargeSlots;
    [SerializeField] Controller player;

    private int chargeLeft = 5;
    private int chargeMax = 5;
    
    public bool UseDashCharge()
    {
        if (chargeLeft > 0)
        {
            DashChargeSlots[chargeLeft - 1].Use();
            chargeLeft--;
            return true;
        }
        return false;
    }

    public bool RecoverDashCharge(int number)
    {
        bool success = false;
        int recoverLeft = number;
        for (int i = chargeLeft; i < DashChargeSlots.Count; i++)
        {
            if (recoverLeft > 0)
            {
                recoverLeft--;
                chargeLeft++;
                success = true;
                DashChargeSlots[i].Recover(1.0f);
            }
        }

        return success;
    }


    public void RecoverAllDashSlot()
    {
        chargeLeft = player.GetMaxDashCharge();
        for (int i = 0; i < DashChargeSlots.Count; i++)
        {
            DashChargeSlots[i].Recover(1.0f);
        }
    }
}
