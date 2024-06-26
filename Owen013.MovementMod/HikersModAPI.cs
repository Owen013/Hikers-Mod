﻿using HikersMod.Components;

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

    public void UpdateConfig()
    {
        if (ModMain.Instance.ModHelper == null) return;
        Config.UpdateConfig(ModMain.Instance.ModHelper.Config);
    }
}