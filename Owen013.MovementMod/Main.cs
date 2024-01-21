using HarmonyLib;
using HikersMod.APIs;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

namespace HikersMod;

public class Main : ModBehaviour
{
    public static Main Instance;
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
    public string SprintMode;
    public string SprintButtonMode;
    public float SprintSpeed;
    public float SprintStrafeSpeed;
    public bool ShouldSprintOnLanding;
    public bool IsSprintEffectEnabled;
    public bool IsEmergencyBoostEnabled;
    public float EmergencyBoostPower;
    public float EmergencyBoostCost;
    public float EmergencyBoostVolume;
    public float EmergencyBoostInputTime;
    public float EmergencyBoostCameraShakeAmount;
    public bool IsFloatyPhysicsEnabled;
    public float FloatyPhysicsMinAccel;
    public float FloatyPhysicsMaxGravity;
    public float FloatyPhysicsMinGravity;
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
    public string WallJumpMode;
    public float WallJumpsPerJump;
    public bool IsDebugLogEnabled;

    public void DebugLog(string text, MessageType type = MessageType.Message, bool forceMessage = false)
    {
        if (!IsDebugLogEnabled && !forceMessage) return;
        ModHelper.Console.WriteLine(text, type);
    }

    public float GetAnimSpeed()
    {
        return _animSpeed;
    }

    public override object GetApi()
    {
        return new HikersModAPI();
    }

    public override void Configure(IModConfig config)
    {
        // Update the config
        JumpStyle = config.GetSettingsValue<string>("Jump Style");

        SprintMode = config.GetSettingsValue<string>("Enable Sprinting");
        SprintButtonMode = config.GetSettingsValue<string>("Sprint Button");
        SprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
        SprintStrafeSpeed = config.GetSettingsValue<float>("Sprint Strafe Speed");
        ShouldSprintOnLanding = config.GetSettingsValue<bool>("Start Sprinting On Landing");
        IsSprintEffectEnabled = config.GetSettingsValue<bool>("Show Thruster Effect while Sprinting");

        IsEmergencyBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
        EmergencyBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
        EmergencyBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");
        EmergencyBoostVolume = config.GetSettingsValue<float>("Emergency Boost Volume");
        EmergencyBoostInputTime = config.GetSettingsValue<float>("Emergency Boost Input Time");
        EmergencyBoostCameraShakeAmount = config.GetSettingsValue<float>("Emergency Boost Camera Shake Amount");

        IsFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Enable Floaty Physics");
        FloatyPhysicsMinAccel = config.GetSettingsValue<float>("Minimum Acceleration");
        FloatyPhysicsMaxGravity = config.GetSettingsValue<float>("Maximum Gravity");
        FloatyPhysicsMinGravity = config.GetSettingsValue<float>("Minimum Gravity");

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
        WallJumpMode = config.GetSettingsValue<string>("Enable Wall Jumping");
        WallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");
        IsDebugLogEnabled = config.GetSettingsValue<bool>("Enable Debug Log");

        // Warn player if config settings may cause issues
        if (FloatyPhysicsMinAccel != Mathf.Clamp(FloatyPhysicsMinAccel, 0f, GroundAccel))
        {
            DebugLog($"Floaty physics minimum acceleration must be between 0 and {GroundAccel}(Ground Acceleration)! Please change config to avoid bugs and glitchy behavior.", MessageType.Warning, true);
        }
        if (FloatyPhysicsMinGravity != Mathf.Clamp(FloatyPhysicsMinGravity, 0f, FloatyPhysicsMaxGravity))
        {
            DebugLog($"Floaty physics minimum gravity must be between 0 and {FloatyPhysicsMaxGravity}(Maximum Gravity)! Please change config to avoid bugs and glitchy behavior.", MessageType.Warning, true);
        }

        ApplyChanges();

        if (OnConfigure != null)
        {
            OnConfigure();
        }
    }

    private void Awake()
    {
        // Static reference to HikersMod so it can be used in patches.
        Instance = this;
        Harmony.CreateAndPatchAll(typeof(Main));
        Harmony.CreateAndPatchAll(typeof(Components.SpeedController));
    }

    private void Start()
    {
        // Get APIs
        SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
        CameraShakerAPI = ModHelper.Interaction.TryGetModApi<ICameraShaker>("SBtT.CameraShake");

        // Ready!
        ModHelper.Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }

    private void Update()
    {
        if (_characterController == null) return;

        UpdateAnimSpeed();
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart(PlayerCharacterController __instance)
    {
        // Get vars
        Instance._characterController = __instance;
        Instance._animController = __instance.GetComponentInChildren<PlayerAnimController>();
        Instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();

        // Add components to character
        __instance.gameObject.AddComponent<Components.SpeedController>();
        __instance.gameObject.AddComponent<Components.EmergencyBoostController>();
        __instance.gameObject.AddComponent<Components.FloatyPhysicsController>();
        __instance.gameObject.AddComponent<Components.WallJumpController>();

        Instance.ApplyChanges();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCloneController), nameof(PlayerCloneController.Start))]
    private static void EyeCloneStart(PlayerCloneController __instance) => Instance._cloneController = __instance;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EyeMirrorController), nameof(EyeMirrorController.Start))]
    private static void EyeMirrorStart(EyeMirrorController __instance) => Instance._mirrorController = __instance;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
    private static bool UpdateAirControl(PlayerCharacterController __instance)
    {
        if (!Instance.IsMidairTurningEnabled) return true;

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerMovementAudio), nameof(PlayerMovementAudio.PlayFootstep))]
    private static bool PlayFootstep(PlayerMovementAudio __instance)
    {
        AudioType audioType = (!PlayerState.IsCameraUnderwater() && __instance._fluidDetector.InFluidType(FluidVolume.Type.WATER)) ? AudioType.MovementShallowWaterFootstep : PlayerMovementAudio.GetFootstepAudioType(__instance._playerController.GetGroundSurface());
        if (audioType != AudioType.None)
        {
            __instance._footstepAudio.pitch = Random.Range(0.9f, 1.1f);
            __instance._footstepAudio.PlayOneShot(audioType, 1.4f * Instance._characterController.GetRelativeGroundVelocity().magnitude / 6);
        }
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
    private static bool OverrideMaxRunSpeed(ref float maxSpeedX, ref float maxSpeedZ, DreamLanternItem __instance)
    {
        float lerpPosition = 1f - __instance._lanternController.GetFocus();
        lerpPosition *= lerpPosition;
        maxSpeedX = Mathf.Lerp(Instance.DreamLanternSpeed, maxSpeedX, lerpPosition);
        maxSpeedZ = Mathf.Lerp(Instance.DreamLanternSpeed, maxSpeedZ, lerpPosition);
        return false;
    }
}