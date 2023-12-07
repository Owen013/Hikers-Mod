using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class SpeedController : MonoBehaviour
    {
        public static SpeedController s_instance;
        public PlayerCharacterController _characterController;
        //public JetpackThrusterController _jetpackController;
        public JetpackThrusterModel _jetpackModel;
        public ThrusterFlameController _forward_left_thruster, _forward_right_thruster, _backward_left_thruster, _backward_right_thruster, _left_thruster, _right_thruster;
        public string _moveSpeed;
        public bool _isDreamLanternFocused;
        public bool _hasDreamLanternFocusChanged;
        //public bool _isDreaming;

        public void Awake()
        {
            s_instance = this;
            Harmony.CreateAndPatchAll(typeof(SpeedController));
            HikersMod.s_instance.OnConfigure += ApplyChanges;
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
            //if (_moveSpeed == "sprinting" || _jetpackController.GetRawInput().magnitude == 0)
            //{
            //    Vector2 _thrusterVector = OWInput.GetAxisValue(InputLibrary.moveXZ);
            //    SetThrusterIntensity(_forward_left_thruster, _thrusterVector.y > 0 ? _thrusterVector.y : 0);
            //    SetThrusterIntensity(_forward_right_thruster, _thrusterVector.y > 0 ? _thrusterVector.y : 0);
            //    SetThrusterIntensity(_backward_left_thruster, _thrusterVector.y < 0 ? -_thrusterVector.y : 0);
            //    SetThrusterIntensity(_backward_right_thruster, _thrusterVector.y < 0 ? -_thrusterVector.y : 0);
            //    SetThrusterIntensity(_left_thruster, _thrusterVector.x > 0 ? _thrusterVector.x : 0);
            //    SetThrusterIntensity(_right_thruster, _thrusterVector.x < 0 ? -_thrusterVector.x : 0);
            //}
        }

        public void ApplyChanges()
        {
            if (!_characterController) return;

            // Change built-in character attributes
            _characterController._runSpeed = HikersMod.s_instance._normalSpeed;
            _characterController._strafeSpeed = HikersMod.s_instance._strafeSpeed;
            _characterController._walkSpeed = HikersMod.s_instance._walkSpeed;
            _characterController._airSpeed = HikersMod.s_instance._airSpeed;
            _characterController._airAcceleration = HikersMod.s_instance._airAccel;

            if (HikersMod.s_instance._sprintButtonMode == "Down Thrust") HikersMod.s_instance._sprintButton = InputLibrary.thrustDown;
            else HikersMod.s_instance._sprintButton = InputLibrary.thrustUp;

            ChangeMoveSpeed();
        }

        public void ChangeMoveSpeed()
        {
            string oldSpeed = _moveSpeed;
            bool holdingLantern = _characterController._heldLanternItem != null;
            bool walking = OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern;
            bool grounded = _characterController._isGrounded && !_characterController.IsSlidingOnIce();
            bool notInDifferentMoveState = !walking && !_isDreamLanternFocused;
            bool sprintAllowed = HikersMod.s_instance._sprintEnabledMode == "Always" || (HikersMod.s_instance._sprintEnabledMode == "When Suited" && PlayerState.IsWearingSuit());

            if (OWInput.IsPressed(HikersMod.s_instance._sprintButton) && grounded && notInDifferentMoveState && sprintAllowed && (OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0 || _moveSpeed == "sprinting"))
            {
                _moveSpeed = "sprinting";
                _characterController._runSpeed = HikersMod.s_instance._sprintSpeed;
                _characterController._strafeSpeed = HikersMod.s_instance._sprintStrafeSpeed;
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
                _characterController._runSpeed = HikersMod.s_instance._normalSpeed;
                _characterController._strafeSpeed = HikersMod.s_instance._strafeSpeed;
            }
            if (_moveSpeed != oldSpeed) HikersMod.s_instance.DebugLog($"Changed movement speed to {_moveSpeed}");
        }

        //public void SetThrusterIntensity(ThrusterFlameController thruster, float intensity)
        //{
        //    intensity = thruster._scaleSpring.Update(thruster.transform.localScale.magnitude, intensity, Time.deltaTime);
        //    thruster.transform.localScale = Vector3.one * intensity;
        //    thruster._light.range = thruster._baseLightRadius * intensity;
        //    thruster._thrusterRenderer.enabled = (intensity > 0f);
        //    thruster._light.enabled = (intensity > 0f);
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void OnCharacterControllerStart()
        {
            s_instance._characterController = FindObjectOfType<PlayerCharacterController>();
            //s_instance._jetpackController = FindObjectOfType<JetpackThrusterController>();
            s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
            s_instance._characterController.OnBecomeGrounded += s_instance.ChangeMoveSpeed;
            //s_instance._isDreaming = false;
            var thrusters = Resources.FindObjectsOfTypeAll<ThrusterFlameController>();
            for (int i = 0; i < thrusters.Length; i++)
            {
                switch(thrusters[i]._thruster)
                {
                    case Thruster.Forward_LeftThruster: s_instance._forward_left_thruster = thrusters[i]; break;
                    case Thruster.Forward_RightThruster: s_instance._forward_right_thruster = thrusters[i]; break;
                    case Thruster.Backward_LeftThruster: s_instance._backward_left_thruster = thrusters[i]; break;
                    case Thruster.Backward_RightThruster: s_instance._backward_right_thruster = thrusters[i]; break;
                    case Thruster.Left_Thruster: s_instance._left_thruster = thrusters[i]; break;
                    case Thruster.Right_Thruster: s_instance._right_thruster = thrusters[i]; break;
                }
            };
            s_instance.ApplyChanges();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
        public static void OnGetJetpackInput(ref Vector3 __result)
        {
            if (HikersMod.s_instance._sprintButton == InputLibrary.thrustDown && s_instance._moveSpeed == "sprinting" && __result.y < 0 ||
            HikersMod.s_instance._sprintButton == InputLibrary.thrustUp && s_instance._moveSpeed == "sprinting" && __result.y > 0)
            {
                __result.y = 0;
                s_instance._jetpackModel._boostActivated = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.UpdateFocus))]
        public static void OnDreamLanternFocusChanged(DreamLanternItem __instance)
        {
            if (__instance._wasFocusing == __instance._focusing) return;
            s_instance._isDreamLanternFocused = __instance._focusing;
            s_instance._hasDreamLanternFocusChanged = true;
            if (__instance._focusing) HikersMod.s_instance.DebugLog("Focused Dream Lantern", OWML.Common.MessageType.Info);
            else HikersMod.s_instance.DebugLog("Unfocused Dream Lantern", OWML.Common.MessageType.Info);
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnEnterDreamWorld))]
        //public static void OnEnteredDreamWorld()
        //{
        //    s_instance._isDreaming = true;
        //    s_instance.ChangeMoveSpeed();
        //    HikersMod.s_instance.DebugLog("Entered Dream World", OWML.Common.MessageType.Info);
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnExitDreamWorld))]
        //public static void OnExitedDreamWorld()
        //{
        //    s_instance._isDreaming = false;
        //    s_instance.ChangeMoveSpeed();
        //    HikersMod.s_instance.DebugLog("Left Dream World", OWML.Common.MessageType.Info);
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
        public static bool CharacterControllerUpdate(PlayerCharacterController __instance)
        {
            if (!__instance._isAlignedToForce && !__instance._isZeroGMovementEnabled)
            {
                return false;
            }
            if ((OWInput.GetValue(InputLibrary.thrustUp, InputMode.All) == 0f) || (HikersMod.s_instance._sprintButton == InputLibrary.thrustUp && s_instance._moveSpeed == "sprinting"))
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
            __result = !PlayerState.InZeroG() && !Locator.GetPlayerSuit().IsTrainingSuit() && !__instance._cameraFluidDetector.InFluidType(FluidVolume.Type.WATER) && __instance._currentFuel > 0f && s_instance._moveSpeed != "sprinting";
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
                __instance._footstepAudio.PlayOneShot(audioType, 1.4f * s_instance._characterController.GetRelativeGroundVelocity().magnitude / 6);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
        public static bool OverrideMaxRunSpeed(ref float maxSpeedX, ref float maxSpeedZ, DreamLanternItem __instance)
        {
            float num = 1f - __instance._lanternController.GetFocus();
            num *= num;
            maxSpeedX = Mathf.Lerp(HikersMod.s_instance._dreamLanternSpeed, maxSpeedX, num);
            maxSpeedZ = Mathf.Lerp(HikersMod.s_instance._dreamLanternSpeed, maxSpeedZ, num);
            return false;
        }
    }
}
