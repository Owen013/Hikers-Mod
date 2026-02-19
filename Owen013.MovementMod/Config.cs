using OWML.Common;
using System;

namespace HikersMod;

public static class Config
{
    public static bool UseChargeJump { get; private set; }

    public static bool IsMidairTurningEnabled { get; private set; }

    public static bool IsSprintingEnabled { get; private set; }

    public static string SprintButton { get; private set; }

    public static float SprintMultiplier { get; private set; }

    public static bool ShouldSprintOnLanding { get; private set; }

    public static bool IsSprintEffectEnabled { get; private set; }

    public static bool SpeedUpGhostsWhileSprinting { get; private set; }

    public static bool IsEmergencyBoostEnabled { get; private set; }

    public static float EmergencyBoostPower { get; private set; }

    public static float EmergencyBoostCost { get; private set; }

    public static float EmergencyBoostVolume { get; private set; }

    public static float EmergencyBoostInputTime { get; private set; }

    public static float EmergencyBoostCameraShakeAmount { get; private set; }

    public static bool IsFloatyPhysicsEnabled { get; private set; }

    public static float FloatyPhysicsMinAccel { get; private set; }

    public static float FloatyPhysicsMaxGravity { get; private set; }

    public static float FloatyPhysicsMinGravity { get; private set; }

    public static bool IsWallJumpingEnabled { get; private set; }

    public static int MaxWallJumps { get; private set; }

    public static float RunSpeed { get; private set; }

    public static float StrafeSpeed { get; private set; }

    public static float WalkSpeed { get; private set; }

    public static float DreamLanternSpeed { get; private set; }

    public static float GroundAccel { get; private set; }

    public static float AirSpeed { get; private set; }

    public static float AirAccel { get; private set; }

    public static float MinJumpPower { get; private set; }

    public static float MaxJumpPower { get; private set; }

    public static float JetpackAccel { get; private set; }

    public static float JetpackBoostAccel { get; private set; }

    public static float JetpackBoostTime { get; private set; }

    public static float MaxJetpackFuel { get; private set; }

    public static event Action OnConfigure;

    public static void Configure(IModConfig config)
    {
        UseChargeJump = config.GetSettingsValue<string>("Jump Style") == "Charge";
        IsMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");

        IsSprintingEnabled = config.GetSettingsValue<bool>("Enable Sprinting");
        SprintButton = config.GetSettingsValue<string>("Sprint Button");
        SprintMultiplier = config.GetSettingsValue<float>("SprintMultiplier");
        ShouldSprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        IsSprintEffectEnabled = config.GetSettingsValue<bool>("Show Thruster Effect while Sprinting");
        SpeedUpGhostsWhileSprinting = config.GetSettingsValue<bool>("Speed Up Ghosts while Sprinting");

        IsEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        EmergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        EmergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        EmergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        EmergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        EmergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");

        IsFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Enable Floaty Physics");
        FloatyPhysicsMinAccel = config.GetSettingsValue<float>("Floaty Physics Minimum Acceleration");
        FloatyPhysicsMaxGravity = config.GetSettingsValue<float>("Floaty Physics Minimum Gravity");
        FloatyPhysicsMinGravity = config.GetSettingsValue<float>("Minimum Gravity");

        IsWallJumpingEnabled = config.GetSettingsValue<bool>("Enable Wall Jumping");
        MaxWallJumps = config.GetSettingsValue<int>("Maximum Number of Wall Jumps");

        RunSpeed = config.GetSettingsValue<float>("Normal Speed");
        StrafeSpeed = config.GetSettingsValue<float>("Strafe Speed");
        WalkSpeed = config.GetSettingsValue<float>("Walk Speed");
        DreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed");
        GroundAccel = config.GetSettingsValue<float>("Ground Acceleration");
        AirSpeed = config.GetSettingsValue<float>("Air Speed");
        AirAccel = config.GetSettingsValue<float>("Air Acceleration");
        MinJumpPower = config.GetSettingsValue<float>("Minimum Jump Power");
        MaxJumpPower = config.GetSettingsValue<float>("Maximum Jump Power");
        JetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration");
        JetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration");
        JetpackBoostTime = config.GetSettingsValue<float>("Max Jetpack Boost Time");
        MaxJetpackFuel = config.GetSettingsValue<float>("Max Jetpack Fuel Amount");

        if (ModMain.SmolHatchlingAPI != null && ModMain.SmolHatchlingAPI.UseScaledPlayerAttributes())
        {
            float playerScale = ModMain.SmolHatchlingAPI.GetPlayerTargetScale();
            RunSpeed *= playerScale;
            StrafeSpeed *= playerScale;
            WalkSpeed *= playerScale;
            AirSpeed *= playerScale;
            GroundAccel *= playerScale;
            AirAccel *= playerScale;
            MinJumpPower *= playerScale;
            MaxJumpPower *= playerScale;
            // jetpack acceleration doesn't need to be scaled, as Smol Hatchling accesses that method directly
            EmergencyBoostPower *= playerScale;
        }

        OnConfigure?.Invoke();
    }
}
