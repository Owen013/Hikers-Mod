using HikersMod.Components;
using static HikersMod.ModMain;

namespace HikersMod;

public class HikersModAPI
{
    public bool IsSprinting()
    {
        return SprintingController.Instance.IsSprinting;
    }

    public void UpdateConfig()
    {
        if (ModConfig != null)
            Config.Configure(ModConfig);
    }
}