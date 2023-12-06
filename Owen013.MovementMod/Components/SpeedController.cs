using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class SpeedController : MonoBehaviour
    {
        public static SpeedController Instance;
        public PlayerCharacterController _characterController;
        public JetpackThrusterModel _jetpackModel;
        public string _moveSpeed;
        public bool _isDreamLanternFocused;
        public bool _hasDreamLanternFocusChanged;
        public bool _isDreaming;

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(SpeedController));
            HikersMod.Instance.OnConfigure += ApplyChanges;
        }

        public void Update()
        {
            if (!_characterController) return;

            bool rollInputChanged = OWInput.IsNewlyPressed(InputLibrary.rollMode) || OWInput.IsNewlyReleased(InputLibrary.rollMode);
            bool downInputChanged = OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown);
            bool upInputChanged = OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyReleased(InputLibrary.thrustUp);
            bool boostedInMidair = OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded();
            if (rollInputChanged || downInputChanged || upInputChanged || boostedInMidair || _hasDreamLanternFocusChanged) ChangeMoveSpeed();

            _hasDreamLanternFocusChanged = false;
        }

        public void ApplyChanges()
        {
            if (!_characterController) return;

            // Change built-in character attributes
            _characterController._runSpeed = HikersMod.Instance._normalSpeed;
            _characterController._strafeSpeed = HikersMod.Instance._strafeSpeed;
            _characterController._walkSpeed = HikersMod.Instance._walkSpeed;
            _characterController._airSpeed = HikersMod.Instance._airSpeed;
            _characterController._airAcceleration = HikersMod.Instance._airAccel;

            if (HikersMod.Instance._sprintButtonMode == "Down Thrust") HikersMod.Instance._sprintButton = InputLibrary.thrustDown;
            else HikersMod.Instance._sprintButton = InputLibrary.thrustUp;

            ChangeMoveSpeed();
        }

        public void ChangeMoveSpeed()
        {
            string oldSpeed = _moveSpeed;
            bool holdingLantern = _characterController._heldLanternItem != null;
            bool walking = OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern;
            bool grounded = _characterController._isGrounded && !_characterController.IsSlidingOnIce();
            bool notInDifferentMoveState = !walking && !_isDreamLanternFocused;
            bool sprintAllowed = HikersMod.Instance._sprintEnabledMode == "Always" || (HikersMod.Instance._sprintEnabledMode == "When Awake" && !_isDreaming);

            if (OWInput.IsPressed(HikersMod.Instance._sprintButton) && grounded && notInDifferentMoveState && sprintAllowed && (OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0 || _moveSpeed == "sprinting"))
            {
                _moveSpeed = "sprinting";
                _characterController._runSpeed = HikersMod.Instance._sprintSpeed;
                _characterController._strafeSpeed = HikersMod.Instance._sprintStrafeSpeed;
            }
            else if (walking)
            {
                _moveSpeed = "walking";
            }
            else if (_isDreamLanternFocused)
            {
                _moveSpeed = "dream_lantern";
            }
            else
            {
                _moveSpeed = "normal";
                _characterController._runSpeed = HikersMod.Instance._normalSpeed;
                _characterController._strafeSpeed = HikersMod.Instance._strafeSpeed;
            }
            if (_moveSpeed != oldSpeed) HikersMod.Instance.DebugLog($"Changed movement speed to {_moveSpeed}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void OnCharacterControllerStart()
        {
            Instance._characterController = FindObjectOfType<PlayerCharacterController>();
            Instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
            Instance._characterController.OnBecomeGrounded += Instance.ChangeMoveSpeed;
            Instance._isDreaming = false;
            Instance.ApplyChanges();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
        public static void OnGetJetpackInput(ref Vector3 __result)
        {
            if (HikersMod.Instance._sprintButton == InputLibrary.thrustDown && Instance._moveSpeed == "sprinting" && __result.y < 0 ||
            HikersMod.Instance._sprintButton == InputLibrary.thrustUp && Instance._moveSpeed == "sprinting" && __result.y > 0)
            {
                __result.y = 0;
                Instance._jetpackModel._boostActivated = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.UpdateFocus))]
        public static void OnDreamLanternFocusChanged(DreamLanternItem __instance)
        {
            if (__instance._wasFocusing == __instance._focusing) return;
            Instance._isDreamLanternFocused = __instance._focusing;
            Instance._hasDreamLanternFocusChanged = true;
            if (__instance._focusing) HikersMod.Instance.DebugLog("Focused Dream Lantern", OWML.Common.MessageType.Info);
            else HikersMod.Instance.DebugLog("Unfocused Dream Lantern", OWML.Common.MessageType.Info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnEnterDreamWorld))]
        public static void OnEnteredDreamWorld()
        {
            Instance._isDreaming = true;
            Instance.ChangeMoveSpeed();
            HikersMod.Instance.DebugLog("Entered Dream World", OWML.Common.MessageType.Info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnExitDreamWorld))]
        public static void OnExitedDreamWorld()
        {
            Instance._isDreaming = false;
            Instance.ChangeMoveSpeed();
            HikersMod.Instance.DebugLog("Left Dream World", OWML.Common.MessageType.Info);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
        public static bool CharacterControllerUpdate(PlayerCharacterController __instance)
        {
            if (!__instance._isAlignedToForce && !__instance._isZeroGMovementEnabled)
            {
                return false;
            }
            if ((OWInput.GetValue(InputLibrary.thrustUp, InputMode.All) == 0f) || (HikersMod.Instance._sprintButton == InputLibrary.thrustUp && Instance._moveSpeed == "sprinting"))
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
            __result = !PlayerState.InZeroG() && !Locator.GetPlayerSuit().IsTrainingSuit() && !__instance._cameraFluidDetector.InFluidType(FluidVolume.Type.WATER) && __instance._currentFuel > 0f && Instance._moveSpeed != "sprinting";
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
        public static bool OverrideMaxRunSpeed(ref float maxSpeedX, ref float maxSpeedZ, DreamLanternItem __instance)
        {
            float num = 1f - __instance._lanternController.GetFocus();
            num *= num;
            maxSpeedX = Mathf.Lerp(HikersMod.Instance._dreamLanternSpeed, maxSpeedX, num);
            maxSpeedZ = Mathf.Lerp(HikersMod.Instance._dreamLanternSpeed, maxSpeedZ, num);
            return false;
        }
    }
}
