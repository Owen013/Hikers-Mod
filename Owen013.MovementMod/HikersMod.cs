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
        public AssetBundle _textAssets;
        public PlayerCharacterController _characterController;
        public PlayerAnimController _animController;
        public PlayerAudioController _audioController;
        public PlayerImpactAudio _impactAudio;
        public ThrusterFlameController _downThrustFlame;
        public GameObject _superBoostNote;
        public bool _isCharacterLoaded;
        public float _wallJumpsLeft;
        public float _lastWallJumpTime;
        public float _lastWallJumpRefill;
        public IInputCommands _sprintButton;
        public float _animSpeed;
        public PlayerCloneController _cloneController;
        public EyeMirrorController _mirrorController;
        public event ConfigureEvent OnConfigure;

        // Config fields
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
        public bool _canGroundThrustWithSprint;
        public float _sprintSpeed;
        public string _wallJumpEnabledMode;
        public float _wallJumpsPerJump;
        public bool _isFloatyPhysicsEnabled;
        public float _floatyPhysicsPower;
        public bool _isSuperBoostEnabled;
        public float _superBoostPower;

        public delegate void ConfigureEvent();

        public void Awake()
        {
            // Static reference to HikersMod so it can be used in patches.
            Instance = this;
            gameObject.AddComponent<SpeedController>();
            gameObject.AddComponent<SuperBoostController>();
            Harmony.CreateAndPatchAll(typeof(HikersMod));
        }

        public void Start()
        {
            _textAssets = ModHelper.Assets.LoadBundle("Assets/textassets");
            SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
            if (SmolHatchlingAPI != null) SmolHatchlingAPI.SetHikersModEnabled();

            // Set characterLoaded to false whenever a new scene begins loading
            LoadManager.OnStartSceneLoad += (scene, loadScene) => _isCharacterLoaded = false;

            // Ready!
            ModHelper.Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
        }

        public void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (!IsCorrectScene() || !_isCharacterLoaded) return;
            
            // Update everthing else
            UpdateWallJump();
            if (_isFloatyPhysicsEnabled) UpdateAcceleration();
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
            _canGroundThrustWithSprint = config.GetSettingsValue<bool>("Allow Thrusting on Ground with Sprinting Enabled");
            _sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
            _wallJumpEnabledMode = config.GetSettingsValue<string>("Enable Wall Jumping");
            _wallJumpsPerJump = config.GetSettingsValue<float>("Wall Jumps per Jump");
            _isFloatyPhysicsEnabled = config.GetSettingsValue<bool>("Floaty Physics in Low-Gravity");
            _floatyPhysicsPower = config.GetSettingsValue<float>("Floaty Physics Power");
            _isSuperBoostEnabled = config.GetSettingsValue<bool>("Enable Jetpack Super-Boost");
            _superBoostPower = config.GetSettingsValue<float>("Super-Boost Power");

            OnConfigure();
            ChangeAttributes();
        }

        public void OnCharacterStart()
        {
            // Get vars
            _characterController = Locator.GetPlayerController();
            _animController = FindObjectOfType<PlayerAnimController>();
            _audioController = FindObjectOfType<PlayerAudioController>();
            _impactAudio = FindObjectOfType<PlayerImpactAudio>();
            var thrusters = Resources.FindObjectsOfTypeAll<ThrusterFlameController>();
            for (int i = 0; i < thrusters.Length; i++) if (thrusters[i]._thruster == Thruster.Up_LeftThruster) _downThrustFlame = thrusters[i];

            _characterController.OnBecomeGrounded += () =>
            {
                _wallJumpsLeft = _wallJumpsPerJump;
            };

            // The Update() code won't run until after Setup() has at least once
            _isCharacterLoaded = true;
            DebugLog("Character loaded", MessageType.Info);

            Configure(ModHelper.Config);
        }

        public void ChangeAttributes()
        {
            if (!IsCorrectScene() || !_isCharacterLoaded) return;

            // Change built-in character attributes
            _characterController._useChargeJump = _jumpStyle == "Charge";
            if (!_isFloatyPhysicsEnabled) _characterController._acceleration = _groundAccel;
            _characterController._airSpeed = _airSpeed;
            _characterController._airAcceleration = _airAccel;

            if (_sprintButtonMode == "Down Thrust") _sprintButton = InputLibrary.thrustDown;
            else _sprintButton = InputLibrary.thrustUp;

            if (_superBoostNote != null) _superBoostNote.SetActive(_isSuperBoostEnabled);
        }

        public void UpdateAcceleration()
        {
            float gravMultiplier;
            if (_characterController.IsGrounded() && !_characterController.IsSlidingOnIce()) gravMultiplier = Mathf.Min(Mathf.Pow(_characterController.GetNormalAccelerationScalar() / 12, _floatyPhysicsPower), 1);
            else gravMultiplier = 1;
            _characterController._acceleration = _groundAccel * gravMultiplier;
        }

        public void UpdateAnimSpeed()
        {
            float gravMultiplier = _characterController._acceleration / _groundAccel;
            float sizeMultiplier = 1f;
            if (SmolHatchlingAPI != null) sizeMultiplier = SmolHatchlingAPI.GetAnimSpeed();
            float groundSpeedMultiplier = Mathf.Pow(_characterController.GetRelativeGroundVelocity().magnitude / 6 * sizeMultiplier, 0.5f);
            float oldAnimSpeed = _animSpeed;
            if (_characterController.IsGrounded()) _animSpeed = Mathf.Max(groundSpeedMultiplier * gravMultiplier, gravMultiplier);
            else _animSpeed = 1f;
            if (oldAnimSpeed == _animSpeed) return;
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

        public void UpdateWallJump()
        {
            _characterController.UpdatePushable();
            if (((_wallJumpEnabledMode == "When Unsuited" && !PlayerState.IsWearingSuit()) || _wallJumpEnabledMode == "Always") &&
                _characterController._isPushable &&
                !PlayerState.InZeroG() &&
                !_characterController._isGrounded &&
                OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) &&
                _wallJumpsLeft > 0)
            {
                OWRigidbody pushBody = _characterController._pushableBody;
                Vector3 pushPoint = _characterController._pushContactPt;
                Vector3 pointVelocity = pushBody.GetPointVelocity(pushPoint);
                Vector3 climbVelocity = new Vector3(0, _jumpPower * (_wallJumpsLeft / _wallJumpsPerJump), 0);

                if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20) DebugLog("Can't Wall-Jump; going too fast");
                else
                {
                    _characterController._owRigidbody.SetVelocity(pointVelocity);
                    _characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
                    _wallJumpsLeft -= 1;
                    _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                    _lastWallJumpTime = _lastWallJumpRefill = Time.time;
                    DebugLog("Wall-Jumped");
                }
            }

            // Replenish 1 wall jump if the player hasn't done one for five seconds
            if (Time.time - _lastWallJumpRefill > 5 && _wallJumpsLeft < _wallJumpsPerJump)
            {
                _wallJumpsLeft += 1;
                _lastWallJumpRefill = Time.time;
            }

            // Make player play fast freefall animation for one second after each wall jump
            if (Time.time - _lastWallJumpTime < 1) _animController._animator.SetFloat("FreefallSpeed", 100);
        }

        public void PlaceSuperBoostNote()
        {
            if (GameObject.Find("Ship_Body") == null) return;
            GameObject notesObject = Instantiate(GameObject.Find("DeepFieldNotes_2"));
            notesObject.AddComponent<SuperBoostNote>();
        }

        public bool IsCorrectScene()
        {
            OWScene scene = LoadManager.s_currentScene;
            return (scene == OWScene.SolarSystem || scene == OWScene.EyeOfTheUniverse);
        }

        public bool InputChanged(IInputCommands input)
        {
            return OWInput.IsNewlyPressed(input) || OWInput.IsNewlyReleased(input);
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
            Instance.OnCharacterStart();
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
        public static bool OverrideMaxRunSpeed(ref float maxSpeedX, ref float maxSpeedZ, DreamLanternItem __instance)
        {
            float num = 1f - __instance._lanternController.GetFocus();
            num *= num;
            maxSpeedX = Mathf.Lerp(Instance._dreamLanternSpeed, maxSpeedX, num);
            maxSpeedZ = Mathf.Lerp(Instance._dreamLanternSpeed, maxSpeedZ, num);
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