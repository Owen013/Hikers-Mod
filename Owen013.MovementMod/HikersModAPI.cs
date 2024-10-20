using HikersMod.Components;

namespace HikersMod;

public class HikersModAPI
{
    public bool IsSprinting()
    {
        return SprintingController.Instance.IsSprinting;
    }

    public void UpdateConfig()
    {
        if (ModMain.Instance == null || ModMain.Instance.ModHelper == null) return;
        ModMain.Instance.Configure(ModMain.Instance.ModHelper.Config);
    }
}