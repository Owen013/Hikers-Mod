using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace HikersMod.Components;

public class SpeedController : MonoBehaviour
{
    public static SpeedController s_instance;
    private PlayerCharacterController _characterController;
    private JetpackThrusterModel _jetpackModel;
    private JetpackThrusterAudio _jetpackAudio;
    private GameObject _playerVFX;
    private List<ThrusterFlameController> _thrusters;
    private string _moveSpeed;
    private bool _isDreamLanternFocused;
    private bool _hasDreamLanternFocusChanged;
    private IInputCommands _sprintButton;
    private Vector2 _thrusterVector;

    public void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(SpeedController));
        ModController.s_instance.OnConfigure += ApplyChanges;
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

    public void LateUpdate()
    {
        if (!_characterController) return;

        // get thruster vector IF the player is sprinting, otherwise move towards zero
        _thrusterVector = Vector2.MoveTowards(_thrusterVector, _moveSpeed == "sprinting" && _characterController._isWearingSuit ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero, Time.deltaTime * 5);
        Vector2 flameVector = _thrusterVector;

        // adjust vector based on how fast sprinting is compared to normal speed
        flameVector.x *= (ModController.s_instance.StrafeSpeed / ModController.s_instance.StrafeSpeed) - 1;
        if (flameVector.y < 0f) flameVector.y *= (ModController.s_instance.SprintStrafeSpeed / ModController.s_instance.StrafeSpeed) - 1;
        else flameVector.y *= (ModController.s_instance.SprintSpeed / ModController.s_instance.DefaultSpeed) - 1;

        // clamp the vector so it doesn't become too big
        flameVector.x = Mathf.Clamp(flameVector.x, -20, 20);
        flameVector.y = Mathf.Clamp(flameVector.y, -20, 20);

        if (_playerVFX.activeSelf) _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._translationalSource, flameVector.magnitude, -flameVector.x, true);
        foreach (ThrusterFlameController thruster in _thrusters)
        {
            switch (thruster._thruster)
            {
                case Thruster.Forward_LeftThruster:
                    SetThrusterScale(thruster, flameVector.y);
                    break;
                case Thruster.Forward_RightThruster:
                    SetThrusterScale(thruster, flameVector.y);
                    break;
                case Thruster.Left_Thruster:
                    SetThrusterScale(thruster, -flameVector.x);
                    break;
                case Thruster.Right_Thruster:
                    SetThrusterScale(thruster, flameVector.x);
                    break;
                case Thruster.Backward_LeftThruster:
                    SetThrusterScale(thruster, -flameVector.y);
                    break;
                case Thruster.Backward_RightThruster:
                    SetThrusterScale(thruster, -flameVector.y);
                    break;
            }
        }
    }

    public void ApplyChanges()
    {
        if (!_characterController) return;

        // Change built-in character attributes
        _characterController._runSpeed = ModController.s_instance.DefaultSpeed;
        _characterController._strafeSpeed = ModController.s_instance.StrafeSpeed;
        _characterController._walkSpeed = ModController.s_instance.WalkSpeed;
        _characterController._airSpeed = ModController.s_instance.AirSpeed;
        _characterController._airAcceleration = ModController.s_instance.AirAccel;

        _sprintButton = (ModController.s_instance.SprintButtonMode == "Down Thrust") ? InputLibrary.thrustDown : InputLibrary.thrustUp;

        ChangeMoveSpeed();
    }

    public void ChangeMoveSpeed()
    {
        string oldSpeed = _moveSpeed;
        bool holdingLantern = _characterController._heldLanternItem != null;
        bool walking = OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern;
        bool grounded = _characterController._isGrounded && !_characterController.IsSlidingOnIce();
        bool notInDifferentMoveState = !walking && !_isDreamLanternFocused;
        bool sprintAllowed = ModController.s_instance.SprintMode == "Always" || (ModController.s_instance.SprintMode == "When Suited" && PlayerState.IsWearingSuit());

        if (OWInput.IsPressed(_sprintButton) && grounded && notInDifferentMoveState && sprintAllowed && (OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0 || _moveSpeed == "sprinting"))
        {
            _moveSpeed = "sprinting";
            _characterController._runSpeed = ModController.s_instance.SprintSpeed;
            _characterController._strafeSpeed = ModController.s_instance.SprintStrafeSpeed;
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
            _characterController._runSpeed = ModController.s_instance.DefaultSpeed;
            _characterController._strafeSpeed = ModController.s_instance.StrafeSpeed;
        }
        if (_moveSpeed != oldSpeed) ModController.s_instance.DebugLog($"Changed movement speed to {_moveSpeed}");
    }

    public void SetThrusterScale(ThrusterFlameController thruster, float scale)
    {
        // don't let the custom scale be smaller than the actual thruster scale or a
        scale = Mathf.Clamp(scale, thruster._currentScale, 100f);
        thruster.transform.localScale = Vector3.one * scale;
        thruster._light.range = thruster._baseLightRadius * scale;
        thruster._thrusterRenderer.enabled = scale > 0f;
        thruster._light.enabled = scale > 0f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    public static void OnCharacterControllerStart()
    {
        s_instance._characterController = Locator.GetPlayerController();
        s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
        s_instance._jetpackAudio = FindObjectOfType<JetpackThrusterAudio>();
        s_instance._playerVFX = s_instance._characterController.GetComponentInChildren<PlayerParticlesController>(includeInactive: true).gameObject;
        s_instance._characterController.OnBecomeGrounded += s_instance.ChangeMoveSpeed;
        s_instance._thrusters = new(s_instance._characterController.gameObject.GetComponentsInChildren<ThrusterFlameController>(includeInactive: true));
        s_instance._thrusterVector = Vector2.zero;
        s_instance.ApplyChanges();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    public static void OnGetJetpackInput(ref Vector3 __result)
    {
        if (s_instance._moveSpeed == "sprinting" && __result.y != 0)
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
        if (__instance._focusing) ModController.s_instance.DebugLog("Focused Dream Lantern", OWML.Common.MessageType.Info);
        else ModController.s_instance.DebugLog("Unfocused Dream Lantern", OWML.Common.MessageType.Info);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
    public static bool CharacterControllerUpdate(PlayerCharacterController __instance)
    {
        if (!__instance._isAlignedToForce && !__instance._isZeroGMovementEnabled)
        {
            return false;
        }
        if ((OWInput.GetValue(InputLibrary.thrustUp, InputMode.All) == 0f) || (s_instance._sprintButton == InputLibrary.thrustUp && s_instance._moveSpeed == "sprinting"))
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
        maxSpeedX = Mathf.Lerp(ModController.s_instance.DreamLanternSpeed, maxSpeedX, num);
        maxSpeedZ = Mathf.Lerp(ModController.s_instance.DreamLanternSpeed, maxSpeedZ, num);
        return false;
    }
}
