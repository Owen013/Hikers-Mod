using HarmonyLib;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;

namespace HikersMod
{
    public class HikersModController : ModBehaviour
    {
        // Mod fields
        public static HikersModController Instance;
        public ISmolHatchling SmolHatchlingAPI;
        public AssetBundle _textAssets;
        public PlayerCharacterController _characterController;
        public PlayerAnimController _animController;
        public PlayerAudioController _audioController;
        public PlayerImpactAudio _impactAudio;
        public JetpackThrusterController _jetpackController;
        public JetpackThrusterModel _jetpackModel;
        public OWAudioSource _superBoostAudio;
        public ThrusterFlameController _downThrustFlame;
        public GameObject _superBoostNote;
        public PlayerCloneController _cloneController;
        public EyeMirrorController _mirrorController;
        public MoveSpeed _moveSpeed;
        public IInputCommands _sprintButton;
        public bool _isCharacterLoaded, _isSuperBoosting;
        public bool _isVerticalThrustDisabled, _isDreamLanternFocused, _hasDreamLanternFocusChanged, _isDreaming;
        public float _strafeSpeed;
        public float _sprintStrafeSpeed;
        public float _animSpeed;
        public float _wallJumpsLeft;
        public float _lastWallJumpTime;
        public float _lastWallJumpRefill;
        public float _lastBoostInputTime;
        public float _lastBoostTime;

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

        public void Awake()
        {
            // Static reference to HikersMod so it can be used in patches.
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(HikersModController));
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

            // If the input changes for rollmode or thrustdown, or if the dream lantern focus just changed, then call UpdateMoveSpeed()
            if (InputChanged(InputLibrary.rollMode) ||
                InputChanged(InputLibrary.thrustDown) ||
                InputChanged(InputLibrary.thrustUp) ||
                (OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded()) ||
                _hasDreamLanternFocusChanged)
            {
                ChangeMoveSpeed();
            }
            
            // Update everthing else
            UpdateWallJump();
            UpdateSuperBoost();
            if (_isFloatyPhysicsEnabled) UpdateAcceleration();
            UpdateAnimSpeed();
            _hasDreamLanternFocusChanged = false;
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

            ChangeAttributes();
        }

        public void OnCharacterStart()
        {
            // Get vars
            _characterController = Locator.GetPlayerController();
            _animController = FindObjectOfType<PlayerAnimController>();
            _audioController = FindObjectOfType<PlayerAudioController>();
            _impactAudio = FindObjectOfType<PlayerImpactAudio>();
            _jetpackController = FindObjectOfType<JetpackThrusterController>();
            _jetpackModel = FindObjectOfType<JetpackThrusterModel>();
            var thrusters = Resources.FindObjectsOfTypeAll<ThrusterFlameController>();
            for (int i = 0; i < thrusters.Length; i++) if (thrusters[i]._thruster == Thruster.Up_LeftThruster) _downThrustFlame = thrusters[i];

            _characterController.OnBecomeGrounded += () =>
            {
                ChangeMoveSpeed();
                _wallJumpsLeft = _wallJumpsPerJump;
                _isSuperBoosting = false;
            };

            // Create superboost audio source
            _superBoostAudio = new GameObject("HikersMod_SuperBoostAudioSrc").AddComponent<OWAudioSource>();
            _superBoostAudio.transform.parent = _audioController.transform;
            _superBoostAudio.transform.localPosition = new Vector3(0, 0, 1);

            _isDreaming = false;

            // The Update() code won't run until after Setup() has at least once
            _isCharacterLoaded = true;
            DebugLog("Character loaded", MessageType.Info);

            Configure(ModHelper.Config);
        }

        public void ChangeAttributes()
        {
            if (!IsCorrectScene() || !_isCharacterLoaded) return;

            // Strafe speed depends on whether or not slowStrafeDisabled is true
            if (_isSlowStrafeDisabled)
            {
                _strafeSpeed = _normalSpeed;
                _sprintStrafeSpeed = _sprintSpeed;
            }
            else
            {
                _strafeSpeed = _normalSpeed * 2f / 3f;
                _sprintStrafeSpeed = _sprintSpeed * 2f / 3f;
            }

            // Change built-in character attributes
            _characterController._useChargeJump = _jumpStyle == "Charge";
            _characterController._runSpeed = _normalSpeed;
            _characterController._strafeSpeed = _strafeSpeed;
            _characterController._walkSpeed = _walkSpeed;
            if (!_isFloatyPhysicsEnabled) _characterController._acceleration = _groundAccel;
            _characterController._airSpeed = _airSpeed;
            _characterController._airAcceleration = _airAccel;
            _characterController._maxJumpSpeed = _jumpPower;
            _jetpackModel._maxTranslationalThrust = _jetpackAccel;
            _jetpackModel._boostThrust = _jetpackBoostAccel;
            _jetpackModel._boostSeconds = _jetpackBoostTime;

            if (_sprintButtonMode == "Down Thrust") _sprintButton = InputLibrary.thrustDown;
            else _sprintButton = InputLibrary.thrustUp;

            if (_superBoostNote != null) _superBoostNote.SetActive(_isSuperBoostEnabled);

            ChangeMoveSpeed();
        }

        public void ChangeMoveSpeed()
        {
            bool holdingLantern = _characterController._heldLanternItem != null;
            bool walking = OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern;
            MoveSpeed oldSpeed = _moveSpeed;

            if (OWInput.IsPressed(_sprintButton) &&
                _characterController._isGrounded &&
                !_characterController.IsSlidingOnIce() &&
                !walking &&
                !_isDreamLanternFocused &&
                ((_sprintEnabledMode == "Everywhere") || _sprintEnabledMode == "Real World Only" && !_isDreaming) &&
                (OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0 || !_characterController._isWearingSuit || !_canGroundThrustWithSprint || _moveSpeed == MoveSpeed.Sprinting))
            {
                _moveSpeed = MoveSpeed.Sprinting;
                _characterController._runSpeed = _sprintSpeed;
                _characterController._strafeSpeed = _sprintStrafeSpeed;
                _isVerticalThrustDisabled = true;
            }
            else if (walking)
            {
                _moveSpeed = MoveSpeed.Walking;
                _isVerticalThrustDisabled = false;
            }
            else if (_isDreamLanternFocused)
            {
                _moveSpeed = MoveSpeed.DreamLantern;
                _isVerticalThrustDisabled = false;
            }
            else
            {
                _moveSpeed = MoveSpeed.Normal;
                _characterController._runSpeed = _normalSpeed;
                _characterController._strafeSpeed = _strafeSpeed;
                _isVerticalThrustDisabled = false;
            }
            if (_moveSpeed != oldSpeed) DebugLog($"Changed movement speed to {_moveSpeed}");
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
            float groundSpeed = _characterController.GetRelativeGroundVelocity().magnitude;
            float gravMultiplier = _characterController._acceleration / _groundAccel;
            float sizeMultiplier = 1f;
            if (SmolHatchlingAPI != null) sizeMultiplier = SmolHatchlingAPI.GetAnimSpeed();
            float oldAnimSpeed = _animSpeed;
            if (_characterController.IsGrounded()) _animSpeed = Mathf.Max(groundSpeed / (6 / sizeMultiplier) * gravMultiplier, gravMultiplier);
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

        public void UpdateSuperBoost()
        {
            bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && (!OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character));
            bool meetsCriteria = _characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
            if (!meetsCriteria) _isSuperBoosting = false;
            else if (isInputting && meetsCriteria && _jetpackController._resources.GetFuel() > 0 && Time.time - _lastBoostInputTime < 0.25f && _isSuperBoostEnabled && !_isSuperBoosting)
            {
                _lastBoostTime = Time.time;
                _isSuperBoosting = true;
                _jetpackModel._boostChargeFraction = 1f;
                _superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, 1f);
                DebugLog("Super-Boosted");
            }
            if (isInputting && meetsCriteria) _lastBoostInputTime = Time.time;
            if (_isSuperBoosting)
            {
                if (_jetpackModel._boostChargeFraction > 0)
                {
                    _jetpackModel._boostActivated = true;
                    _jetpackController._translationalInput.y = 1;
                }
                else
                {
                    _jetpackController._translationalInput.y = 0;
                }
                _jetpackModel._boostThrust = _jetpackBoostAccel * _superBoostPower;
                _jetpackModel._boostSeconds = _jetpackBoostTime / _superBoostPower;
                _jetpackModel._chargeSeconds = float.PositiveInfinity;
                _downThrustFlame._currentScale = Mathf.Max(_downThrustFlame._currentScale, Mathf.Max(2 - (Time.time - _lastBoostTime), 0) * 7.5f * _jetpackModel._boostChargeFraction);
            }
            else
            {
                _jetpackModel._boostThrust = _jetpackBoostAccel;
                _jetpackModel._boostSeconds = _jetpackBoostTime;
                if (_characterController.IsGrounded()) _jetpackModel._chargeSeconds = _jetpackModel._chargeSecondsGround;
                else _jetpackModel._chargeSeconds = _jetpackModel._chargeSecondsAir;
            }
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
        public static void GetJetpackInput(ref Vector3 __result)
        {
            if (Instance._sprintButton == InputLibrary.thrustDown && Instance._isVerticalThrustDisabled && __result.y < 0 ||
            Instance._sprintButton == InputLibrary.thrustUp && Instance._isVerticalThrustDisabled && __result.y > 0)
            {
                __result.y = 0;
                Instance._jetpackModel._boostActivated = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.UpdateFocus))]
        public static void DreamLanternFocusChanged(DreamLanternItem __instance)
        {
            if (__instance._wasFocusing == __instance._focusing) return;
            Instance._isDreamLanternFocused = __instance._focusing;
            Instance._hasDreamLanternFocusChanged = true;
            if (__instance._focusing) Instance.DebugLog("Focused Dream Lantern", MessageType.Info);
            else Instance.DebugLog("Unfocused Dream Lantern", MessageType.Info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnEnterDreamWorld))]
        public static void EnteredDreamWorld()
        {
            Instance._isDreaming = true;
            Instance.ChangeMoveSpeed();
            Instance.DebugLog("Entered Dream World", MessageType.Info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnExitDreamWorld))]
        public static void ExitedDreamWorld()
        {
            Instance._isDreaming = false;
            Instance.ChangeMoveSpeed();
            Instance.DebugLog("Left Dream World", MessageType.Info);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
        public static bool CharacterControllerUpdate(PlayerCharacterController __instance)
        {
            if (!__instance._isAlignedToForce && !__instance._isZeroGMovementEnabled)
            {
                return false;
            }
            if ((OWInput.GetValue(InputLibrary.thrustUp, InputMode.All) == 0f) || (Instance._sprintButton == InputLibrary.thrustUp && Instance._isVerticalThrustDisabled))
            {
                __instance.UpdateJumpInput();
            }
            else
            {
                __instance._jumpChargeTime = 0f;
                __instance._jumpNextFixedUpdate = false;
                __instance._jumpPressedInOtherMode = false;
            }
            if (__instance._isZeroGMovementEnabled)
            {
                __instance._pushPrompt.SetVisibility(OWInput.IsInputMode(InputMode.Character | InputMode.NomaiRemoteCam) && __instance._isPushable);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.IsBoosterAllowed))]
        public static bool IsBoosterAllowed(ref bool __result, PlayerResources __instance)
        {
            __result = !PlayerState.InZeroG() && !Locator.GetPlayerSuit().IsTrainingSuit() && !__instance._cameraFluidDetector.InFluidType(FluidVolume.Type.WATER) && __instance._currentFuel > 0f && !Instance._isVerticalThrustDisabled;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMovementAudio), nameof(PlayerMovementAudio.PlayFootstep))]
        public static bool PlayFootstep(PlayerMovementAudio __instance)
        {
            AudioType audioType = (!PlayerState.IsCameraUnderwater() && __instance._fluidDetector.InFluidType(FluidVolume.Type.WATER)) ? AudioType.MovementShallowWaterFootstep : PlayerMovementAudio.GetFootstepAudioType(__instance._playerController.GetGroundSurface());
            if (audioType != AudioType.None)
            {
                __instance._footstepAudio.pitch = Random.Range(0.9f, 1.1f);
                if (Instance._moveSpeed == MoveSpeed.Sprinting) __instance._footstepAudio.PlayOneShot(audioType, 1.4f);
                else __instance._footstepAudio.PlayOneShot(audioType, 0.7f);
            }
            return false;
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