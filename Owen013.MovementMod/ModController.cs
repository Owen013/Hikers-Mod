using HarmonyLib;
using HikersMod.APIs;
using HikersMod.Components;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

namespace HikersMod;

public class ModController : ModBehaviour
{
    public static ModController s_instance;
    public ISmolHatchling SmolHatchlingAPI;
    public ICameraShaker CameraShakerAPI;
    private PlayerCharacterController _characterController;
    private PlayerAnimController _animController;
    private JetpackThrusterModel _jetpackModel;
    private PlayerCloneController _cloneController;
    private EyeMirrorController _mirrorController;
    public delegate void ConfigureEvent();
    public event ConfigureEvent OnConfigure;
    private float _animSpeed;

    // Config
    public string JumpStyle;
    public string SprintMode;
    public string SprintButtonMode;
    public float SprintSpeed;
    public float SprintStrafeSpeed;
    public bool SprintOnLanding;
    public bool IsEmergencyBoostEnabled;
    public float EmergencyBoostPower;
    public float EmergencyBoostCost;
    public float EmergencyBoostVolume;
    public float EmergencyBoostInputTime;
    public float EmergencyBoostCameraShakeAmount;
    public float DefaultSpeed;
    public float StrafeSpeed;
    public float WalkSpeed;
    public float DreamLanternSpeed;
    public float GroundAccel;
    public float AirSpeed;
    public float AirAccel;
    public float JumpPower;
    public float JetpackAccel;
    public float JetpackBoostAccel;
    public float JetpackBoostTime;
    public bool IsMidairTurningEnabled;
    public bool IsFloatyPhysicsEnabled;
    public float FloatyPhysicsPower;
    public string WallJumpMode;
    public float WallJumpsPerJump;
    public bool DebugLogEnabled;

    public override object GetApi()
    {
        return new HikersModAPI();
    }

    private void Awake()
    {
        // Static reference to HikersMod so it can be used in patches.
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(ModController));
        gameObject.AddComponent<SpeedController>();
        gameObject.AddComponent<EmergencyBoostController>();
        gameObject.AddComponent<FloatyPhysicsController>();
        gameObject.AddComponent<WallJumpController>();
    }

    private void Start()
    {
        // Get APIs
        SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
        SmolHatchlingAPI?.SetHikersModEnabled();
        CameraShakerAPI = ModHelper.Interaction.TryGetModApi<ICameraShaker>("SBtT.CameraShake");

        // Ready!
        ModHelper.Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }

    private void Update()
    {
        if (_characterController == null) return;

        UpdateAnimSpeed();
    }

    public override void Configure(IModConfig config)
    {
        // Get all config options
        JumpStyle = config.GetSettingsValue<string>("Jump Style");
        SprintMode = config.GetSettingsValue<string>("Enable Sprinting");
        SprintButtonMode = config.GetSettingsValue<string>("Sprint Button");
        SprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
        SprintStrafeSpeed = config.GetSettingsValue<float>("Sprint Strafe Speed");
        SprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        IsEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        EmergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        EmergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        EmergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        EmergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        EmergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");
        DefaultSpeed = config.GetSettingsValue<float>("Normal Speed");
        StrafeSpeed = config.GetSettingsValue<float>("Strafe Speed");
        WalkSpeed = config.GetSettingsValue<float>("Walk Speed");
        DreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed");
        GroundAccel = config.GetSettingsValue<float>("Ground Acceleration");
        AirSpeed = config.GetSettingsValue<float>("Air Speed");
        AirAccel = config.GetSettingsValue<float>("Air Acceleration");
        JumpPower = config.GetSettingsValue<float>("Jump Power");
        JetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration");
        JetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration");
        JetpackBoostTime = config.GetSettingsValue<float>("Max Jetpack Boost Time");
        IsMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");
        IsFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Floaty Physics");
        FloatyPhysicsPower = config.GetSettingsValue<float>("Floaty Physics Power");
        WallJumpMode = config.GetSettingsValue<string>("Enable Wall Jumping");
        WallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");
        DebugLogEnabled = config.GetSettingsValue<bool>("Enable Debug Log");

        ApplyChanges();
        OnConfigure();
    }

    private void ApplyChanges()
    {
        if (_characterController == null) return;

        // Change built-in character attributes
        _characterController._useChargeJump = JumpStyle == "Charge";
        if (!IsFloatyPhysicsEnabled) _characterController._acceleration = GroundAccel;
        _characterController._airSpeed = AirSpeed;
        _characterController._airAcceleration = AirAccel;
        _jetpackModel._maxTranslationalThrust = JetpackAccel;
        _jetpackModel._boostThrust = JetpackBoostAccel;
        _jetpackModel._boostSeconds = JetpackBoostTime;
    }

    private void UpdateAnimSpeed()
    {
        float speedMultiplier = Mathf.Pow(_characterController.GetRelativeGroundVelocity().magnitude / 6 * (SmolHatchlingAPI != null ? SmolHatchlingAPI.GetAnimSpeed() : 1), 0.5f);
        float gravMultiplier = Mathf.Sqrt(_characterController._acceleration / GroundAccel);

        _animSpeed = _characterController.IsGrounded() ? Mathf.Max(speedMultiplier * gravMultiplier, gravMultiplier) : 1f;
        _animController._animator.speed = _animSpeed;

        // copy the new anim speed to the clone and mirror reflection
        if (_cloneController != null)
        {
            _cloneController._playerVisuals.GetComponent<PlayerAnimController>()._animator.speed = _animSpeed;
        }
        if (_mirrorController != null)
        {
            _mirrorController._mirrorPlayer.GetComponentInChildren<PlayerAnimController>()._animator.speed = _animSpeed;
        }
    }

    public float GetAnimSpeed()
    {
        return _animSpeed;
    }

    public void DebugLog(string text, MessageType type = MessageType.Message, bool forceMessage = false)
    {
        if (!DebugLogEnabled && !forceMessage) return;
        ModHelper.Console.WriteLine(text, type);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart()
    {
        // Get vars
        s_instance._characterController = Locator.GetPlayerController();
        s_instance._animController = FindObjectOfType<PlayerAnimController>();
        s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();

        s_instance.ApplyChanges();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
    private static bool UpdateAirControl(PlayerCharacterController __instance)
    {
        if (!s_instance.IsMidairTurningEnabled) return true;

        if (__instance._lastGroundBody != null)
        {
            // get player's horizontal velocity
            Vector3 pointVelocity = __instance._transform.InverseTransformDirection(__instance._lastGroundBody.GetPointVelocity(__instance._transform.position));
            Vector3 localVelocity = __instance._transform.InverseTransformDirection(__instance._owRigidbody.GetVelocity()) - pointVelocity;
            localVelocity.y = 0f;

            float physicsTime = Time.fixedDeltaTime * 60f;
            float maxChange = __instance._airAcceleration * physicsTime;

            Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);
            Vector3 localVelocityChange = new(maxChange * axisValue.x, 0f, maxChange * axisValue.y);

            // new velocity can't be more than old velocity and airspeed
            float maxSpeed = Mathf.Max(localVelocity.magnitude, __instance._airSpeed);
            Vector3 newLocalVelocity = Vector3.ClampMagnitude(localVelocity + localVelocityChange, maxSpeed);

            // cancel out old velocity, add new one
            __instance._owRigidbody.AddLocalVelocityChange(-localVelocity + newLocalVelocity);
        }
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCloneController), nameof(PlayerCloneController.Start))]
    private static void EyeCloneStart(PlayerCloneController __instance) => s_instance._cloneController = __instance;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EyeMirrorController), nameof(EyeMirrorController.Start))]
    private static void EyeMirrorStart(EyeMirrorController __instance) => s_instance._mirrorController = __instance;
}