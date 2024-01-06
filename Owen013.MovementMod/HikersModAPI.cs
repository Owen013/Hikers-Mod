using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public bool IsSprinting()
    {
        return SpeedController.s_instance.IsSprinting();
    }

    public bool IsEmergencyBoosting()
    {
        return EmergencyBoostController.s_instance.IsEmergencyBoosting();
    }
}