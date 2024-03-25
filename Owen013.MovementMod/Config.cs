using OWML.Common;

namespace HikersMod;

public static class Config
{
    public static bool useChargeJump { get; private set; }
    public static bool isSprintingEnabled { get; private set; }
    public static string sprintButton { get; private set; }
    public static bool isMidairTurningEnabled { get; private set; }
    public static bool isEmergencyBoostEnabled { get; private set; }
    public static bool isFloatyPhysicsEnabled { get; private set; }
    public static string wallJumpMode { get; private set; }
    public static float runSpeed { get; private set; }
    public static float strafeSpeed { get; private set; }
    public static float walkSpeed { get; private set; }
    public static float dreamLanternSpeed { get; private set; }
    public static float groundAccel { get; private set; }
    public static float airSpeed { get; private set; }
    public static float airAccel { get; private set; }
    public static float minJumpPower { get; private set; }
    public static float maxJumpPower { get; private set; }
    public static float jetpackAccel { get; private set; }
    public static float jetpackBoostAccel { get; private set; }
    public static float jetpackBoostTime { get; private set; }
    public static float sprintMultiplier { get; private set; }
    public static bool shouldSprintOnLanding { get; private set; }
    public static bool isSprintEffectEnabled { get; private set; }
    public static float emergencyBoostPower { get; private set; }
    public static float emergencyBoostCost { get; private set; }
    public static float emergencyBoostVolume { get; private set; }
    public static float emergencyBoostInputTime { get; private set; }
    public static float emergencyBoostCameraShakeAmount { get; private set; }
    public static float floatyPhysicsMinAccel { get; private set; }
    public static float floatyPhysicsMaxGravity { get; private set; }
    public static float floatyPhysicsMinGravity { get; private set; }
    public static float wallJumpsPerJump { get; private set; }

    public static void UpdateConfig(IModConfig config)
    {
        useChargeJump = config.GetSettingsValue<string>("Jump Style") == "Charge";
        isSprintingEnabled = config.GetSettingsValue<bool>("Enable Sprinting");
        sprintButton = config.GetSettingsValue<string>("Sprint Button");
        isMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");

        isEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        isFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Enable Floaty Physics");
        wallJumpMode = config.GetSettingsValue<string>("Enable Wall Jumping");

        runSpeed = config.GetSettingsValue<float>("Normal Speed");
        strafeSpeed = config.GetSettingsValue<float>("Strafe Speed");
        walkSpeed = config.GetSettingsValue<float>("Walk Speed");
        dreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed");
        groundAccel = config.GetSettingsValue<float>("Ground Acceleration");
        airSpeed = config.GetSettingsValue<float>("Air Speed");
        airAccel = config.GetSettingsValue<float>("Air Acceleration");
        minJumpPower = config.GetSettingsValue<float>("Minimum Jump Power");
        maxJumpPower = config.GetSettingsValue<float>("Maximum Jump Power");
        jetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration");
        jetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration");
        jetpackBoostTime = config.GetSettingsValue<float>("Max Jetpack Boost Time");
        sprintMultiplier = config.GetSettingsValue<float>("SprintMultiplier");
        shouldSprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        isSprintEffectEnabled = config.GetSettingsValue<bool>("Show Thruster Effect while Sprinting");
        emergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        emergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        emergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        emergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        emergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");
        floatyPhysicsMinAccel = config.GetSettingsValue<float>("Floaty Physics Minimum Acceleration");
        floatyPhysicsMaxGravity = config.GetSettingsValue<float>("Floaty Physics Minimum Gravity");
        floatyPhysicsMinGravity = config.GetSettingsValue<float>("Minimum Gravity");
        wallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");
    }
}