using HarmonyLib;
using HikersMod.APIs;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace HikersMod;

public class Main : ModBehaviour
{
    public static Main Instance { get; private set; }
    public ISmolHatchling SmolHatchlingAPI { get; private set; }
    public ICameraShaker CameraShakerAPI { get; private set; }

    public delegate void ConfigureEvent();
    public event ConfigureEvent OnConfigure;

    public override void Configure(IModConfig config)
    {
        // Update the config
        Config.useChargeJump = config.GetSettingsValue<string>("Jump Style") == "Charge";
        Config.sprintMode = config.GetSettingsValue<string>("Enable Sprinting");
        Config.sprintButton = config.GetSettingsValue<string>("Sprint Button");
        Config.sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
        Config.sprintStrafeSpeed = config.GetSettingsValue<float>("Sprint Strafe Speed");
        Config.shouldSprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        Config.isSprintEffectEnabled = config.GetSettingsValue<bool>("Show Thruster Effect while Sprinting");
        Config.isEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        Config.emergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        Config.emergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        Config.emergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        Config.emergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        Config.emergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");
        Config.TripDuration = config.GetSettingsValue<float>("Trip Duration");
        Config.TripChance = config.GetSettingsValue<float>("Chance of Tripping Randomly");
        Config.SprintingTripChance = config.GetSettingsValue<float>("Chance of Tripping while Sprinting");
        Config.DamagedTripChance = config.GetSettingsValue<float>("Chance of Tripping when Damaged");
        Config.ReverseBoostChance = config.GetSettingsValue<float>("Reverse Boost Chance");
        Config.EmergencyBoostMisfireChance = config.GetSettingsValue<float>("Emergency Boost Misfire Chance");
        Config.ScoutMisfireChance = config.GetSettingsValue<float>("Scout Misfire Chance");
        Config.ReverseRepairChance = config.GetSettingsValue<float>("Reverse Repair Chance");
        Config.isFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Enable Floaty Physics");
        Config.floatyPhysicsMinAccel = config.GetSettingsValue<float>("Minimum Acceleration");
        Config.floatyPhysicsMaxGravity = config.GetSettingsValue<float>("Maximum Gravity");
        Config.floatyPhysicsMinGravity = config.GetSettingsValue<float>("Minimum Gravity");
        Config.defaultSpeed = config.GetSettingsValue<float>("Normal Speed");
        Config.strafeSpeed = config.GetSettingsValue<float>("Strafe Speed");
        Config.walkSpeed = config.GetSettingsValue<float>("Walk Speed");
        Config.dreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed");
        Config.groundAccel = config.GetSettingsValue<float>("Ground Acceleration");
        Config.airSpeed = config.GetSettingsValue<float>("Air Speed");
        Config.airAccel = config.GetSettingsValue<float>("Air Acceleration");
        Config.minJumpPower = config.GetSettingsValue<float>("Minimum Jump Power");
        Config.maxJumpPower = config.GetSettingsValue<float>("Maximum Jump Power");
        Config.jetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration");
        Config.jetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration");
        Config.jetpackBoostTime = config.GetSettingsValue<float>("Max Jetpack Boost Time");
        Config.isMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");
        Config.wallJumpMode = config.GetSettingsValue<string>("Enable Wall Jumping");
        Config.wallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");

        OnConfigure?.Invoke();
    }

    private void Awake()
    {
        // Static reference to HikersMod so it can be used in patches.
        Instance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        // Get APIs
        SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
        CameraShakerAPI = ModHelper.Interaction.TryGetModApi<ICameraShaker>("SBtT.CameraShake");

        // Ready!
        WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }

    public static void WriteLine(string text, MessageType type = MessageType.Message)
    {
        // null check because this method is created before ModHelper is defined!
        if (Instance.ModHelper == null) return;

        Instance.ModHelper.Console.WriteLine(text, type);
    }
}