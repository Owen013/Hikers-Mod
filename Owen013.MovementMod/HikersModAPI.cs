using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public float GetAnimSpeed()
    {
        return Main.Instance.GetAnimSpeed();
    }

    public bool IsSprinting()
    {
        return SpeedController.Instance.IsSprinting();
    }

    public bool IsEmergencyBoosting()
    {
        return EmergencyBoostController.Instance.IsEmergencyBoosting();
    }
}