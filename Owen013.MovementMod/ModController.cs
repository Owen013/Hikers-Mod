using HarmonyLib;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using HikersMod.APIs;

namespace HikersMod;

public class ModController : ModBehaviour
{
    public static ModController s_instance;
    public ISmolHatchling SmolHatchlingAPI;
    public ICameraShaker CameraShakerAPI;
    public delegate void ConfigureEvent();
    public event ConfigureEvent OnConfigure;
    private PlayerCharacterController _characterController;
    private PlayerAnimController _animController;
    private JetpackThrusterModel _jetpackModel;
    private PlayerCloneController _cloneController;
    private EyeMirrorController _mirrorController;
    private float _animSpeed;

    // Config
    public string JumpStyle;
    public bool IsMidairTurningEnabled;
    public string SprintMode;
    public string SprintButtonMode;
    public bool CanSprintBackwards;
    public float SprintSpeed;
    public float SprintStrafeSpeed;
    public bool IsSuperBoostEnabled;
    public float SuperBoostPower;
    public float SuperBoostCost;
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
    public bool IsFloatyPhysicsEnabled;
    public float FloatyPhysicsPower;
    public string WallJumpMode;
    public float WallJumpsPerJump;
    public bool DebugLogEnabled;

    private void Awake()
    {
        // Static reference to HikersMod so it can be used in patches.
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(ModController));
        gameObject.AddComponent<Components.SpeedController>();
        gameObject.AddComponent<Components.SuperBoostController>();
        gameObject.AddComponent<Components.FloatyPhysicsController>();
        gameObject.AddComponent<Components.WallJumpController>();
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
        if (!_characterController) return;
        UpdateAnimSpeed();
    }

    public override void Configure(IModConfig config)
    {
        base.Configure(config);

        // Get all config options
        JumpStyle = config.GetSettingsValue<string>("Jump Style");
        IsMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");
        SprintMode = config.GetSettingsValue<string>("Enable Sprinting");
        SprintButtonMode = config.GetSettingsValue<string>("Sprint Button");
        SprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
        SprintStrafeSpeed = config.GetSettingsValue<float>("Sprint Strafe Speed");
        IsSuperBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        SuperBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        SuperBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
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
        if (!_characterController) return;

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
        float gravMultiplier = Mathf.Sqrt(_characterController._acceleration / GroundAccel);
        float sizeMultiplier = SmolHatchlingAPI != null ? SmolHatchlingAPI.GetAnimSpeed() : 1;
        float groundSpeedMultiplier = Mathf.Pow(_characterController.GetRelativeGroundVelocity().magnitude / 6 * sizeMultiplier, 0.5f);
        _animSpeed = _characterController.IsGrounded() ? Mathf.Max(groundSpeedMultiplier * gravMultiplier, gravMultiplier) : 1f;
        _animController._animator.speed = _animSpeed;

        if (_cloneController != null)
        {
            _cloneController._playerVisuals.GetComponent<PlayerAnimController>()._animator.speed = _animSpeed;
        }

        if (_mirrorController != null)
        {
            _mirrorController._mirrorPlayer.GetComponentInChildren<PlayerAnimController>()._animator.speed = _animSpeed;
        }
    }

    public void DebugLog(string text)
    {
        if (!DebugLogEnabled) return;
        ModHelper.Console.WriteLine(text);
    }

    public void DebugLog(string text, MessageType type)
    {
        if (!DebugLogEnabled) return;
        ModHelper.Console.WriteLine(text, type);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    public static void OnCharacterControllerStart()
    {
        // Get vars
        s_instance._characterController = Locator.GetPlayerController();
        s_instance._animController = FindObjectOfType<PlayerAnimController>();
        s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();

        s_instance.ApplyChanges();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
    public static bool UpdateAirControl(PlayerCharacterController __instance)
    {
        if (!s_instance.IsMidairTurningEnabled) return true;
        if (__instance == null) return true;
        if (__instance._lastGroundBody != null)
        {
            Vector3 pointVelocity = __instance._transform.InverseTransformDirection(__instance._lastGroundBody.GetPointVelocity(__instance._transform.position));
            Vector3 localVelocity = __instance._transform.InverseTransformDirection(__instance._owRigidbody.GetVelocity()) - pointVelocity;
            localVelocity.y = 0f;
            float physicsTime = Time.fixedDeltaTime * 60f;
            float maxChange = __instance._airAcceleration * physicsTime;
            Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);
            Vector3 localVelocityChange = new Vector3(maxChange * axisValue.x, 0f, maxChange * axisValue.y);
            Vector3 newLocalVelocity = localVelocity + localVelocityChange;
            if (newLocalVelocity.magnitude > __instance._airSpeed && newLocalVelocity.magnitude > localVelocity.magnitude)
                __instance._owRigidbody.AddLocalVelocityChange(-localVelocity + Vector3.ClampMagnitude(newLocalVelocity, localVelocity.magnitude));
            else __instance._owRigidbody.AddLocalVelocityChange(localVelocityChange);
        }
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCloneController), nameof(PlayerCloneController.Start))]
    public static void EyeCloneStart(PlayerCloneController __instance) => s_instance._cloneController = __instance;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EyeMirrorController), nameof(EyeMirrorController.Start))]
    public static void EyeMirrorStart(EyeMirrorController __instance) => s_instance._mirrorController = __instance;
}