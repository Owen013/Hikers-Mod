using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public bool IsSprinting()
    {
        return SpeedController.Instance.IsSprinting();
    }

    public bool IsEmergencyBoosting()
    {
        return EmergencyBoostController.Instance.IsEmergencyBoosting();
    }
}