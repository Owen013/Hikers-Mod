using OWML.Common;
using UnityEngine;

namespace HikersMod;

public static class Config
{
    public static bool UseChargeJump { get; private set; }

    public static bool IsSprintingEnabled { get; private set; }

    public static string SprintButton { get; private set; }

    public static bool IsMidairTurningEnabled { get; private set; }

    public static bool IsEmergencyBoostEnabled { get; private set; }

    public static bool IsFloatyPhysicsEnabled { get; private set; }

    public static string WallJumpMode { get; private set; }

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

    public static bool IsStaminaEnabled { get; private set; }

    public static float StaminaSeconds { get; private set; }

    public static float StaminaRecoveryRate { get; private set; }

    public static float TiredMultiplier { get; private set; }

    public static bool IsSprintEffectEnabled { get; private set; }

    public static float EmergencyBoostPower { get; private set; }

    public static float EmergencyBoostCost { get; private set; }

    public static float EmergencyBoostVolume { get; private set; }

    public static float EmergencyBoostInputTime { get; private set; }

    public static float EmergencyBoostCameraShakeAmount { get; private set; }

    public static float FloatyPhysicsMinAccel { get; private set; }

    public static float FloatyPhysicsMaxGravity { get; private set; }

    public static float FloatyPhysicsMinGravity { get; private set; }

    public static float MaxWallJumps { get; private set; }

    public delegate void ConfigureEvent();

    public static event ConfigureEvent OnConfigure;

    public static void UpdateConfig(IModConfig config)
    {
        UseChargeJump = config.GetSettingsValue<string>("Jump Style") == "Charge";
        IsSprintingEnabled = config.GetSettingsValue<bool>("Enable Sprinting");
        SprintButton = config.GetSettingsValue<string>("Sprint Button");
        IsMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");
        IsStaminaEnabled = false; // config.GetSettingsValue<bool>("Enable Stamina");
        IsEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        IsFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Enable Floaty Physics");
        WallJumpMode = config.GetSettingsValue<string>("Enable Wall Jumping");
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
        StaminaSeconds = 8f; // config.GetSettingsValue<float>("Seconds of Stamina");
        StaminaRecoveryRate = 1f; // = config.GetSettingsValue<float>("Stamina Recovery Rate");
        TiredMultiplier = 0.75f; // config.GetSettingsValue<float>("TiredMultiplier");
        IsSprintEffectEnabled = config.GetSettingsValue<bool>("Show Thruster Effect while Sprinting");
        EmergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        EmergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        EmergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        EmergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        EmergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");
        FloatyPhysicsMinAccel = config.GetSettingsValue<float>("Floaty Physics Minimum Acceleration");
        FloatyPhysicsMaxGravity = config.GetSettingsValue<float>("Floaty Physics Minimum Gravity");
        FloatyPhysicsMinGravity = config.GetSettingsValue<float>("Minimum Gravity");
        MaxWallJumps = config.GetSettingsValue<float>("Maximum Number of Wall Jumps");

        if (ModMain.Instance.SmolHatchlingAPI != null && ModMain.Instance.SmolHatchlingAPI.UseScaledPlayerAttributes())
        {
            Vector3 playerScale = ModMain.Instance.SmolHatchlingAPI.GetTargetScale();
            RunSpeed *= playerScale.x;
            StrafeSpeed *= playerScale.x;
            WalkSpeed *= playerScale.x;
            DreamLanternSpeed *= playerScale.x;
            AirSpeed *= playerScale.x;
            GroundAccel *= playerScale.x;
            AirAccel *= playerScale.x;
            MinJumpPower *= Mathf.Sqrt(playerScale.y);
            MaxJumpPower *= Mathf.Sqrt(playerScale.y);
        }

        OnConfigure?.Invoke();
    }

    /* stamina config
    
		"Enable Stamina": {
			"tooltip": "Sprinting will now use up stamina, so you won't be able to sprint forever and will have to let your stamina replenish. Recommended: Disabled",
			"type": "toggle",
			"value": false
		},
    
		"Seconds of Stamina": {
			"tooltip": "The amount of time you can sprint before running out of stamina. Recommended: 8",
			"type": "number",
			"value": 8
		},
		"Stamina Recovery Rate": {
			"tooltip": "The amount of seconds of stamina you regain every second. Recommended: 1",
			"type": "number",
			"value": 1
		},
		"TiredMultiplier": {
			"title": "Tired Speed Multiplier",
			"tooltip": "How much your speed is multiplied when tired. Recommended: 0.75",
			"type": "number",
			"value": 0.75
		},

    */
}