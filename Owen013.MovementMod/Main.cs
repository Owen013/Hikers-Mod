using HarmonyLib;
using HikersMod.APIs;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace HikersMod;

public delegate void ConfigureEvent();

public class Main : ModBehaviour
{
    public static Main Instance;
    public event ConfigureEvent OnConfigure;
    public ISmolHatchling SmolHatchlingAPI;
    public ICameraShaker CameraShakerAPI;
    private PlayerCharacterController _characterController;
    private JetpackThrusterModel _jetpackModel;

    public void DebugLog(string text, MessageType type = MessageType.Message, bool forceMessage = false)
    {
        if (!Config.IsDebugLogEnabled && !forceMessage) return;
        ModHelper.Console.WriteLine(text, type);
    }

    public void ApplyCharacterAttributes()
    {
        if (_characterController == null || _jetpackModel == null)
        {
            _characterController = Locator.GetPlayerController();
            _jetpackModel = FindObjectOfType<JetpackThrusterModel>();

            if (_characterController == null || _jetpackModel == null)
            {
                DebugLog("Can't apply changes; PlayerCharacterController is null", MessageType.Error);
                return;
            }
        }

        // Change built-in character attributes
        _characterController._useChargeJump = Config.JumpStyle == "Charge";
        if (!Config.IsFloatyPhysicsEnabled) _characterController._acceleration = Config.GroundAccel;
        _characterController._airSpeed = Config.AirSpeed;
        _characterController._airAcceleration = Config.AirAccel;
        _characterController._minJumpSpeed = Config.MinJumpPower;
        _characterController._maxJumpSpeed = Config.MaxJumpPower;
        _jetpackModel._maxTranslationalThrust = Config.JetpackAccel;
        _jetpackModel._boostThrust = Config.JetpackBoostAccel;
        _jetpackModel._boostSeconds = Config.JetpackBoostTime;
    }

    public override void Configure(IModConfig config)
    {
        // Update the config
        Config.JumpStyle = config.GetSettingsValue<string>("Jump Style");

        Config.SprintMode = config.GetSettingsValue<string>("Enable Sprinting");
        Config.SprintButtonMode = config.GetSettingsValue<string>("Sprint Button");
        Config.SprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
        Config.SprintStrafeSpeed = config.GetSettingsValue<float>("Sprint Strafe Speed");
        Config.ShouldSprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        Config.IsSprintEffectEnabled = config.GetSettingsValue<bool>("Show Thruster Effect while Sprinting");

        Config.IsEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        Config.EmergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        Config.EmergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        Config.EmergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        Config.EmergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        Config.EmergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");

        Config.IsFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Enable Floaty Physics");
        Config.FloatyPhysicsMinAccel = config.GetSettingsValue<float>("Minimum Acceleration");
        Config.FloatyPhysicsMaxGravity = config.GetSettingsValue<float>("Maximum Gravity");
        Config.FloatyPhysicsMinGravity = config.GetSettingsValue<float>("Minimum Gravity");

        Config.DefaultSpeed = config.GetSettingsValue<float>("Normal Speed");
        Config.StrafeSpeed = config.GetSettingsValue<float>("Strafe Speed");
        Config.WalkSpeed = config.GetSettingsValue<float>("Walk Speed");
        Config.DreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed");
        Config.GroundAccel = config.GetSettingsValue<float>("Ground Acceleration");
        Config.AirSpeed = config.GetSettingsValue<float>("Air Speed");
        Config.AirAccel = config.GetSettingsValue<float>("Air Acceleration");
        Config.MinJumpPower = config.GetSettingsValue<float>("Minimum Jump Power");
        Config.MaxJumpPower = config.GetSettingsValue<float>("Maximum Jump Power");
        Config.JetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration");
        Config.JetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration");
        Config.JetpackBoostTime = config.GetSettingsValue<float>("Max Jetpack Boost Time");

        Config.IsMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");
        Config.WallJumpMode = config.GetSettingsValue<string>("Enable Wall Jumping");
        Config.WallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");
        Config.IsDebugLogEnabled = config.GetSettingsValue<bool>("Enable Debug Log");

        ApplyCharacterAttributes();

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
        ModHelper.Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }
}