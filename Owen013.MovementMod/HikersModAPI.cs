using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public bool IsSprinting()
    {
        return SpeedController.s_instance.IsSprinting();
    }
    public bool IsSuperBoosting()
    {
        return SuperBoostController.s_instance.IsSuperBoosting();
    }
}