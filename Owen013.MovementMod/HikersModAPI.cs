using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public float GetAnimSpeed()
    {
        return Main.Instance.GetAnimSpeed();
    }

    public string GetMoveSpeed()
    {
        return SpeedController.Instance.GetMoveSpeed();
    }

    public bool IsEmergencyBoosting()
    {
        return EmergencyBoostController.Instance.IsEmergencyBoosting();
    }
}