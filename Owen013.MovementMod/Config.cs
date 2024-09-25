using OWML.Common;
using UnityEngine;

namespace HikersMod;

public static class Config
{
    public static bool UseChargeJump { get; private set; }

    public static bool IsSprintingEnabled { get; private set; }

    public static string SprintButton { get; private set; }

    public static bool IsMidairTurningEnabled { get; private set; }

    public static bool IsStaminaEnabled { get; private set; }

    public static bool IsEmergencyBoostEnabled { get; private set; }

    public static bool IsFloatyPhysicsEnabled { get; private set; }

    public static bool IsWallJumpingEnabled { get; private set; }

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

    public static float SprintMultiplier { get; private set; }

    public static bool ShouldSprintOnLanding { get; private set; }

    public static float StaminaSeconds { get; private set; }

    public static float StaminaRecoveryRate { get; private set; }

    public static bool IsSprintEffectEnabled { get; private set; }

    public static float EmergencyBoostPower { get; private set; }

    public static float EmergencyBoostCost { get; private set; }

    public static float EmergencyBoostVolume { get; private set; }

    public static float EmergencyBoostInputTime { get; private set; }

    public static float EmergencyBoostCameraShakeAmount { get; private set; }

    public static float FloatyPhysicsMinAccel { get; private set; }

    public static float FloatyPhysicsMaxGravity { get; private set; }

    public static float FloatyPhysicsMinGravity { get; private set; }

    public delegate void ConfigureEvent();

    public static event ConfigureEvent OnConfigure;

    public static void UpdateConfig(IModConfig config)
    {
        UseChargeJump = config.GetSettingsValue<string>("Jump Style") == "Charge";
        IsSprintingEnabled = config.GetSettingsValue<bool>("Enable Sprinting");
        SprintButton = config.GetSettingsValue<string>("Sprint Button");
        IsMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");

        IsStaminaEnabled = config.GetSettingsValue<bool>("Enable Stamina");
        IsEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        IsFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Enable Floaty Physics");
        IsWallJumpingEnabled = config.GetSettingsValue<bool>("Enable Wall Jumping");

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

        SprintMultiplier = config.GetSettingsValue<float>("SprintMultiplier");
        ShouldSprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        StaminaSeconds = config.GetSettingsValue<float>("Seconds of Stamina");
        StaminaRecoveryRate =  config.GetSettingsValue<float>("Stamina Recovery Rate");
        IsSprintEffectEnabled = config.GetSettingsValue<bool>("Show Thruster Effect while Sprinting");

        EmergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        EmergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        EmergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        EmergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        EmergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");

        FloatyPhysicsMinAccel = config.GetSettingsValue<float>("Floaty Physics Minimum Acceleration");
        FloatyPhysicsMaxGravity = config.GetSettingsValue<float>("Floaty Physics Minimum Gravity");
        FloatyPhysicsMinGravity = config.GetSettingsValue<float>("Minimum Gravity");

        if (ModMain.Instance.SmolHatchlingAPI != null && ModMain.Instance.SmolHatchlingAPI.UseScaledPlayerAttributes())
        {
            float playerScale = ModMain.Instance.SmolHatchlingAPI.GetPlayerTargetScale();
            RunSpeed = 6 * playerScale;
            StrafeSpeed = 4 * playerScale;
            WalkSpeed = 3 * playerScale;
            AirSpeed = 3 * playerScale;
            GroundAccel = 0.5f * playerScale;
            AirAccel = 0.09f * playerScale;
            MinJumpPower = 3 * playerScale;
            MaxJumpPower = 7 * playerScale;
        }

        OnConfigure?.Invoke();
    }
}