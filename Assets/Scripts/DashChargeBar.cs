using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashChargeBar : MonoBehaviour
{
    [SerializeField] List<DashChargeSlot> DashChargeSlots;
    [SerializeField] Controller player;
    
    public bool UseDashCharge()
    {
        for (int i = DashChargeSlots.Count-1; i >= 0; i--)
        {
            if (!DashChargeSlots[i].IsUsed())
            {
                DashChargeSlots[i].Use();
                return true;
            }
        }
        return false;
    }

    public bool RecoverDashCharge(int number, bool instant)
    {
        bool success = false;
        int recoverLeft = number;

        for (int i = 0; i < DashChargeSlots.Count; i++)
        {
            if (DashChargeSlots[i].IsUsed() && recoverLeft > 0)
            {
                recoverLeft--;
                success = true;

                float animTime = instant ? 0.0f : 1.0f;
                DashChargeSlots[i].Recover(1.0f, animTime);
            }
        }

        return success;
    }
    
    public void RecoverAllDashSlot(bool instant)
    {
        float animTime = instant ? 0.0f : 1.0f;
        for (int i = 0; i < DashChargeSlots.Count; i++)
        {
            if (DashChargeSlots[i].IsUsed())
            {
                DashChargeSlots[i].Recover(1.0f, animTime);
            }
        }
    }
}
