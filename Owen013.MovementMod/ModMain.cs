using HarmonyLib;
using HikersMod.Components;
using HikersMod.Interfaces;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;

namespace HikersMod;

[HarmonyPatch]
public class ModMain : ModBehaviour
{
    public static ModMain Instance { get; private set; }

    public ISmolHatchling SmolHatchlingAPI { get; private set; }

    public ICameraShaker CameraShakerAPI { get; private set; }
    
    public IImmersion ImmersionAPI { get; private set; }

    public bool UseChargeJump { get; private set; }

    public bool IsSprintingEnabled { get; private set; }

    public string SprintButton { get; private set; }

    public bool IsMidairTurningEnabled { get; private set; }

    public bool IsEmergencyBoostEnabled { get; private set; }

    public bool IsFloatyPhysicsEnabled { get; private set; }

    public bool IsWallJumpingEnabled { get; private set; }

    public float RunSpeed { get; private set; }

    public float StrafeSpeed { get; private set; }

    public float WalkSpeed { get; private set; }

    public float DreamLanternSpeed { get; private set; }

    public float GroundAccel { get; private set; }

    public float AirSpeed { get; private set; }

    public float AirAccel { get; private set; }

    public float MinJumpPower { get; private set; }

    public float MaxJumpPower { get; private set; }

    public float JetpackAccel { get; private set; }

    public float JetpackBoostAccel { get; private set; }

    public float JetpackBoostTime { get; private set; }

    public float MaxJetpackFuel { get; private set; }

    public float SprintMultiplier { get; private set; }

    public bool ShouldSprintOnLanding { get; private set; }

    public bool IsSprintEffectEnabled { get; private set; }

    public float EmergencyBoostPower { get; private set; }

    public float EmergencyBoostCost { get; private set; }

    public float EmergencyBoostVolume { get; private set; }

    public float EmergencyBoostInputTime { get; private set; }

    public float EmergencyBoostCameraShakeAmount { get; private set; }

    public float FloatyPhysicsMinAccel { get; private set; }

    public float FloatyPhysicsMaxGravity { get; private set; }

    public float FloatyPhysicsMinGravity { get; private set; }

    public static float MaxWallJumps { get; private set; }

    public delegate void ConfigureEvent();

    public event ConfigureEvent OnConfigure;

    public override object GetApi()
    {
        return new HikersModAPI();
    }

    public override void Configure(IModConfig config)
    {
        base.Configure(config);

        UseChargeJump = config.GetSettingsValue<string>("Jump Style") == "Charge";
        IsSprintingEnabled = config.GetSettingsValue<bool>("Enable Sprinting");
        SprintButton = config.GetSettingsValue<string>("Sprint Button");
        IsMidairTurningEnabled = config.GetSettingsValue<bool>("Enable Midair Turning");

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

        if (SmolHatchlingAPI != null && SmolHatchlingAPI.UseScaledPlayerAttributes())
        {
            float playerScale = SmolHatchlingAPI.GetPlayerTargetScale();
            RunSpeed *= playerScale;
            StrafeSpeed *= playerScale;
            WalkSpeed *= playerScale;
            AirSpeed *= playerScale;
            GroundAccel *= playerScale;
            AirAccel *= playerScale;
            MinJumpPower *= playerScale;
            MaxJumpPower *= playerScale;
            // jetpack acceleration doesn't need to be scaled, and Smol Hatchling accesses that method directly
            EmergencyBoostPower *= playerScale;
        }

        OnConfigure?.Invoke();
    }

    public void WriteLine(string text, MessageType type = MessageType.Message)
    {
        Instance.ModHelper.Console.WriteLine(text, type);
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
        ImmersionAPI = ModHelper.Interaction.TryGetModApi<IImmersion>("Owen_013.FirstPersonPresence");

        // Ready!
        WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }

    // Add components to character
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart(PlayerCharacterController __instance)
    {
        __instance.gameObject.AddComponent<CharacterAttributeController>();
        __instance.gameObject.AddComponent<SprintingController>();
        __instance.gameObject.AddComponent<EmergencyBoostController>();
        __instance.gameObject.AddComponent<FloatyPhysicsController>();
        __instance.gameObject.AddComponent<WallJumpController>();
        __instance.gameObject.AddComponent<JetpackSprintEffectController>();
    }

    // allows turning in midair
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
    private static bool UpdateAirControl(PlayerCharacterController __instance)
    {
        // if feature is disabled then just do the vanilla method
        if (!ModMain.Instance.IsMidairTurningEnabled) return true;

        if (__instance._lastGroundBody != null)
        {
            // get player's horizontal velocity
            Vector3 pointVelocity = __instance._transform.InverseTransformDirection(__instance._lastGroundBody.GetPointVelocity(__instance._transform.position));
            Vector3 localVelocity = __instance._transform.InverseTransformDirection(__instance._owRigidbody.GetVelocity()) - pointVelocity;
            localVelocity.y = 0f;

            float physicsTime = Time.fixedDeltaTime * 60f;
            float acceleration = __instance._airAcceleration * physicsTime;
            Vector2 moveInput = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);
            Vector3 localVelocityChange = new(acceleration * moveInput.x, 0f, acceleration * moveInput.y);

            // new velocity can't be more than old velocity and airspeed
            float maxSpeed = Mathf.Max(localVelocity.magnitude, __instance._airSpeed);
            Vector3 newLocalVelocity = Vector3.ClampMagnitude(localVelocity + localVelocityChange, maxSpeed);

            // cancel out old velocity, add new one
            __instance._owRigidbody.AddLocalVelocityChange(newLocalVelocity - localVelocity);
        }
        return false;
    }

    // adjusts footstep sound based on player speed
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerMovementAudio), nameof(PlayerMovementAudio.PlayFootstep))]
    private static bool PlayFootstep(PlayerMovementAudio __instance)
    {
        AudioType audioType = (!PlayerState.IsCameraUnderwater() && __instance._fluidDetector.InFluidType(FluidVolume.Type.WATER)) ? AudioType.MovementShallowWaterFootstep : PlayerMovementAudio.GetFootstepAudioType(__instance._playerController.GetGroundSurface());
        if (audioType != AudioType.None)
        {
            __instance._footstepAudio.pitch = Random.Range(0.9f, 1.1f);
            float audioVolume = 1.4f * Locator.GetPlayerController().GetRelativeGroundVelocity().magnitude / 6f;
            if (ModMain.Instance.SmolHatchlingAPI != null)
            {
                audioVolume /= ModMain.Instance.SmolHatchlingAPI.GetPlayerScale();
            }
            __instance._footstepAudio.PlayOneShot(audioType, audioVolume);
        }
        return false;
    }

    // changes dream lantern max speed to config setting
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
    private static bool OverrideMaxRunSpeed(ref float maxSpeedX, ref float maxSpeedZ, DreamLanternItem __instance)
    {
        float lerpPosition = 1f - __instance._lanternController.GetFocus();
        lerpPosition *= lerpPosition;
        maxSpeedX = Mathf.Lerp(ModMain.Instance.DreamLanternSpeed, maxSpeedX, lerpPosition);
        maxSpeedZ = Mathf.Lerp(ModMain.Instance.DreamLanternSpeed, maxSpeedZ, lerpPosition);
        return false;
    }
}