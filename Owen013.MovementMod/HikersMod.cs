using HarmonyLib;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using HikersMod.Components;

namespace HikersMod
{
    public class HikersMod : ModBehaviour
    {
        // Mod fields
        public static HikersMod Instance;
        public ISmolHatchling SmolHatchlingAPI;
        public PlayerCharacterController _characterController;
        public PlayerAnimController _animController;
        public IInputCommands _sprintButton;
        public float _animSpeed;
        public PlayerCloneController _cloneController;
        public EyeMirrorController _mirrorController;
        public event ConfigureEvent OnConfigure;

        // Config
        public bool _isDebugLogEnabled;
        public float _normalSpeed;
        public float _walkSpeed;
        public float _dreamLanternSpeed;
        public float _groundAccel;
        public float _airSpeed;
        public float _airAccel;
        public float _jumpPower;
        public float _jetpackAccel;
        public float _jetpackBoostAccel;
        public float _jetpackBoostTime;
        public string _jumpStyle;
        public bool _isSlowStrafeDisabled;
        public bool _enhancedAirControlEnabled;
        public string _sprintEnabledMode;
        public string _sprintButtonMode;
        public bool _canSprintBackwards;
        public bool _canGroundThrustWithSprint;
        public float _sprintSpeed;
        public string _wallJumpEnabledMode;
        public float _wallJumpsPerJump;
        public bool _isFloatyPhysicsEnabled;
        public float _floatyPhysicsPower;
        public bool _isSuperBoostEnabled;
        public float _superBoostPower;
        public float _superBoostCost;

        public delegate void ConfigureEvent();

        public void Awake()
        {
            // Static reference to HikersMod so it can be used in patches.
            Instance = this;
            gameObject.AddComponent<SpeedController>();
            gameObject.AddComponent<WallJumpController>();
            gameObject.AddComponent<FloatyPhysicsController>();
            gameObject.AddComponent<SuperBoostController>();
            Harmony.CreateAndPatchAll(typeof(HikersMod));
        }

        public void Start()
        {
            SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
            if (SmolHatchlingAPI != null) SmolHatchlingAPI.SetHikersModEnabled();

            // Ready!
            ModHelper.Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
        }

        public void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (!_characterController) return;
            
            // Update everthing else
            UpdateAnimSpeed();
        }

        public override void Configure(IModConfig config)
        {
            base.Configure(config);

            // Get all settings values
            _isDebugLogEnabled = config.GetSettingsValue<bool>("Enable Debug Log");
            _normalSpeed = config.GetSettingsValue<float>("Normal Speed");
            _walkSpeed = config.GetSettingsValue<float>("Walk Speed");
            _dreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed");
            _groundAccel = config.GetSettingsValue<float>("Ground Acceleration");
            _airSpeed = config.GetSettingsValue<float>("Air Speed");
            _airAccel = config.GetSettingsValue<float>("Air Acceleration");
            _jumpPower = config.GetSettingsValue<float>("Jump Power");
            _jumpStyle = config.GetSettingsValue<string>("Jump Style");
            _jetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration");
            _jetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration");
            _jetpackBoostTime = config.GetSettingsValue<float>("Max Jetpack Boost Time");
            _isSlowStrafeDisabled = config.GetSettingsValue<bool>("Disable Strafing Slowdown");
            _enhancedAirControlEnabled = config.GetSettingsValue<bool>("Enable Enhanced Air Control");
            _sprintEnabledMode = config.GetSettingsValue<string>("Enable Sprinting");
            _sprintButtonMode = config.GetSettingsValue<string>("Sprint Button");
            _canSprintBackwards = config.GetSettingsValue<bool>("Allow Sprinting Backwards");
            _canGroundThrustWithSprint = config.GetSettingsValue<bool>("Allow Thrusting on Ground with Sprinting Enabled");
            _sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
            _wallJumpEnabledMode = config.GetSettingsValue<string>("Enable Wall Jumping");
            _wallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");
            _isFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Floaty Physics in Low-Gravity");
            _floatyPhysicsPower = config.GetSettingsValue<float>("Floaty Physics Power");
            _isSuperBoostEnabled = config.GetSettingsValue<bool>("Enable Emergency Boost");
            _superBoostPower = config.GetSettingsValue<float>("Emergency Boost Power");
            _superBoostCost = config.GetSettingsValue<float>("Emergency Boost Cost");

            ApplyChanges();
            OnConfigure();
        }

        public void ApplyChanges()
        {
            if (!_characterController) return;

            // Change built-in character attributes
            _characterController._useChargeJump = _jumpStyle == "Charge";
            if (!_isFloatyPhysicsEnabled) _characterController._acceleration = _groundAccel;
            _characterController._airSpeed = _airSpeed;
            _characterController._airAcceleration = _airAccel;

            if (_sprintButtonMode == "Down Thrust") _sprintButton = InputLibrary.thrustDown;
            else _sprintButton = InputLibrary.thrustUp;
        }

        public void UpdateAnimSpeed()
        {
            float gravMultiplier = Mathf.Sqrt(_characterController._acceleration / _groundAccel);
            float sizeMultiplier = SmolHatchlingAPI != null ? SmolHatchlingAPI.GetAnimSpeed() : 1;
            float groundSpeedMultiplier = Mathf.Pow(_characterController.GetRelativeGroundVelocity().magnitude / 6 * sizeMultiplier, 0.5f);
            _animSpeed = _characterController.IsGrounded() ? Mathf.Max(groundSpeedMultiplier * gravMultiplier, gravMultiplier) : 1f;
            _animController._animator.speed = _animSpeed;
            DebugLog("UpdatedAnimSpeed");

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
            if (!_isDebugLogEnabled) return;
            ModHelper.Console.WriteLine(text);
        }

        public void DebugLog(string text, MessageType type)
        {
            if (!_isDebugLogEnabled) return;
            ModHelper.Console.WriteLine(text, type);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void CharacterControllerStart()
        {
            // Get vars
            Instance._characterController = Locator.GetPlayerController();
            Instance._animController = FindObjectOfType<PlayerAnimController>();

            Instance.ApplyChanges();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
        public static bool CharacterUpdateAirControl(PlayerCharacterController __instance)
        {
            if (!Instance._enhancedAirControlEnabled) return true;
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
        public static void EyeCloneStart(PlayerCloneController __instance) => Instance._cloneController = __instance;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EyeMirrorController), nameof(EyeMirrorController.Start))]
        public static void EyeMirrorStart(EyeMirrorController __instance) => Instance._mirrorController = __instance;

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAnimController), nameof(PlayerAnimController.LateUpdate))]
        public static void Animator_GetLayerWeight(PlayerAnimController __instance)
        {
            __instance._animator.SetLayerWeight(1, 1);
        }
        */
    }
}