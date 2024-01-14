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
    public string jumpStyle;
    public string sprintMode;
    public string sprintButtonMode;
    public float sprintSpeed;
    public float sprintStrafeSpeed;
    public bool sprintOnLanding;
    public bool isEmergencyBoostEnabled;
    public float emergencyBoostPower;
    public float emergencyBoostCost;
    public float emergencyBoostVolume;
    public float emergencyBoostInputTime;
    public float emergencyBoostCameraShakeAmount;
    public bool isViewBobbingEnabled;
    public float viewBobXSensitivity;
    public float viewBobYSensitivity;
    public float toolBobSensitivity;
    public float defaultSpeed;
    public float strafeSpeed;
    public float walkSpeed;
    public float dreamLanternSpeed;
    public float groundAccel;
    public float airSpeed;
    public float airAccel;
    public float jumpPower;
    public float jetpackAccel;
    public float jetpackBoostAccel;
    public float jetpackBoostTime;
    public bool isMidairTurningEnabled;
    public bool isFloatyPhysicsEnabled;
    public float floatyPhysicsPower;
    public string wallJumpMode;
    public float wallJumpsPerJump;
    public bool debugLogEnabled;

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
        gameObject.AddComponent<ViewBobController>();
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
        jumpStyle = config.GetSettingsValue<string>("Jump Style");
        sprintMode = config.GetSettingsValue<string>("Enable Sprinting");
        sprintButtonMode = config.GetSettingsValue<string>("Sprint Button");
        sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
        sprintStrafeSpeed = config.GetSettingsValue<float>("Sprint Strafe Speed");
        sprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        isEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        emergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        emergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        emergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        emergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        emergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");
        viewBobXSensitivity = config.GetSettingsValue<float>("View Bob X Sensitivity");
        viewBobYSensitivity = config.GetSettingsValue<float>("View Bob Y Sensitivity");
        toolBobSensitivity = config.GetSettingsValue<float>("Tool Bob Sensitivity");
        defaultSpeed = config.GetSettingsValue<float>("Normal Speed");
        strafeSpeed = config.GetSettingsValue<float>("Strafe Speed");
        walkSpeed = config.GetSettingsValue<float>("Walk Speed");
        dreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed");
        groundAccel = config.GetSettingsValue<float>("Ground Acceleration");
        airSpeed = config.GetSettingsValue<float>("Air Speed");
        airAccel = config.GetSettingsValue<float>("Air Acceleration");
        jumpPower = config.GetSettingsValue<float>("Jump Power");
        jetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration");
        jetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration");
        jetpackBoostTime = config.GetSettingsValue<float>("Max Jetpack Boost Time");
        isMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");
        isFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Floaty Physics");
        floatyPhysicsPower = config.GetSettingsValue<float>("Floaty Physics Power");
        wallJumpMode = config.GetSettingsValue<string>("Enable Wall Jumping");
        wallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");
        debugLogEnabled = config.GetSettingsValue<bool>("Enable Debug Log");

        ApplyChanges();
        OnConfigure();
    }

    private void ApplyChanges()
    {
        if (_characterController == null) return;

        // Change built-in character attributes
        _characterController._useChargeJump = jumpStyle == "Charge";
        if (!isFloatyPhysicsEnabled) _characterController._acceleration = groundAccel;
        _characterController._airSpeed = airSpeed;
        _characterController._airAcceleration = airAccel;
        _jetpackModel._maxTranslationalThrust = jetpackAccel;
        _jetpackModel._boostThrust = jetpackBoostAccel;
        _jetpackModel._boostSeconds = jetpackBoostTime;
    }

    private void UpdateAnimSpeed()
    {
        float speedMultiplier = Mathf.Pow(_characterController.GetRelativeGroundVelocity().magnitude / 6 * (SmolHatchlingAPI != null ? SmolHatchlingAPI.GetAnimSpeed() : 1), 0.5f);
        float gravMultiplier = Mathf.Sqrt(_characterController._acceleration / groundAccel);

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
        if (!debugLogEnabled && !forceMessage) return;
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
        if (!s_instance.isMidairTurningEnabled) return true;

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