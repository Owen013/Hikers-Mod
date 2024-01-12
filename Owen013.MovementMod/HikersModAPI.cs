using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public float GetAnimSpeed()
    {
        return ModController.s_instance.GetAnimSpeed();
    }

    public string GetMoveSpeed()
    {
        return SpeedController.s_instance.GetMoveSpeed();
    }

    public bool IsEmergencyBoosting()
    {
        return EmergencyBoostController.s_instance.IsEmergencyBoosting();
    }
}