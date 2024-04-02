using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public bool IsSprintModeActive()
    {
        return SprintingController.Instance.IsSprintModeActive;
    }

    public bool IsSprinting()
    {
        return SprintingController.Instance.IsSprinting();
    }
}