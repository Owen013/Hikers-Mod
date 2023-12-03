using HarmonyLib;
using UnityEngine;
using static HikersMod.HikersMod;

namespace HikersMod.Components
{
    public class SpeedController : MonoBehaviour
    {
        public static SpeedController Instance;
        public PlayerCharacterController _characterController;
        public PlayerCloneController _cloneController;
        public EyeMirrorController _mirrorController;
        public MoveSpeed _moveSpeed;
        public bool _isVerticalThrustDisabled, _isDreamLanternFocused, _hasDreamLanternFocusChanged, _isDreaming;
        public float _strafeSpeed;
        public float _sprintStrafeSpeed;

        public enum MoveSpeed
        {
            Normal,
            Walking,
            DreamLantern,
            Sprinting
        }

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(SpeedController));
        }

        public void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (!_characterController) return;

            // If the input changes for rollmode or thrustdown, or if the dream lantern focus just changed, then call UpdateMoveSpeed()
            if (OWInput.IsNewlyPressed(InputLibrary.rollMode) || OWInput.IsNewlyReleased(InputLibrary.rollMode) ||
                OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown) ||
                OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyReleased(InputLibrary.thrustUp) ||
                (OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded()) ||
                _hasDreamLanternFocusChanged)
            {
                ChangeMoveSpeed();
            }

            // Update everthing else
            UpdateAnimSpeed();
            _hasDreamLanternFocusChanged = false;
        }

        public void ChangeMoveSpeed()
        {
            bool holdingLantern = _characterController._heldLanternItem != null;
            bool walking = OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern;
            MoveSpeed oldSpeed = _moveSpeed;

            if (OWInput.IsPressed(HikersMod.Instance._sprintButton) &&
                _characterController._isGrounded &&
                !_characterController.IsSlidingOnIce() &&
                !walking &&
                !_isDreamLanternFocused &&
                ((HikersMod.Instance._sprintEnabledMode == "Everywhere") || HikersMod.Instance._sprintEnabledMode == "Real World Only" && !_isDreaming) &&
                (OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0 || !_characterController._isWearingSuit || !HikersMod.Instance._canGroundThrustWithSprint || _moveSpeed == MoveSpeed.Sprinting))
            {
                _moveSpeed = MoveSpeed.Sprinting;
                _characterController._runSpeed = HikersMod.Instance._sprintSpeed;
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
                _characterController._runSpeed = HikersMod.Instance._normalSpeed;
                _characterController._strafeSpeed = _strafeSpeed;
                _isVerticalThrustDisabled = false;
            }
            if (_moveSpeed != oldSpeed) HikersMod.Instance.DebugLog($"Changed movement speed to {_moveSpeed}");
        }

        public void ChangeAttributes()
        {
            if (!_characterController) return;

            // Strafe speed depends on whether or not slowStrafeDisabled is true
            if (HikersMod.Instance._isSlowStrafeDisabled)
            {
                _strafeSpeed = HikersMod.Instance._normalSpeed;
                _sprintStrafeSpeed = HikersMod.Instance._sprintSpeed;
            }
            else
            {
                _strafeSpeed = HikersMod.Instance._normalSpeed * 2f / 3f;
                _sprintStrafeSpeed = HikersMod.Instance._sprintSpeed * 2f / 3f;
            }

            // Change built-in character attributes
            _characterController._runSpeed = HikersMod.Instance._normalSpeed;
            _characterController._strafeSpeed = _strafeSpeed;
            _characterController._walkSpeed = HikersMod.Instance._walkSpeed;
            _characterController._airSpeed = HikersMod.Instance._airSpeed;
            _characterController._airAcceleration = HikersMod.Instance._airAccel;

            if (HikersMod.Instance._sprintButtonMode == "Down Thrust") _sprintButton = InputLibrary.thrustDown;
            else _sprintButton = InputLibrary.thrustUp;

            ChangeMoveSpeed();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void CharacterControllerStart()
        {
            Instance._characterController = FindObjectOfType<PlayerCharacterController>();
            Instance._characterController.OnBecomeGrounded += () =>
            {
                Instance.ChangeMoveSpeed();
            };

            Instance._isDreaming = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
        public static void GetJetpackInput(ref Vector3 __result)
        {
            if (HikersMod.Instance._sprintButton == InputLibrary.thrustDown && Instance._isVerticalThrustDisabled && __result.y < 0 ||
            HikersMod.Instance._sprintButton == InputLibrary.thrustUp && Instance._isVerticalThrustDisabled && __result.y > 0)
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
                __instance._footstepAudio.PlayOneShot(audioType, 1.4f * Instance._characterController.GetRelativeGroundVelocity().magnitude / 6);
            }
            return false;
        }
    }
}
