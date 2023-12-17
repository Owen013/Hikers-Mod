﻿using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace HikersMod.Components;

public class SpeedController : MonoBehaviour
{
    public static SpeedController s_instance;
    public bool IsSprinting;
    private PlayerCharacterController _characterController;
    private JetpackThrusterModel _jetpackModel;
    private JetpackThrusterAudio _jetpackAudio;
    private GameObject _playerVFX;
    private List<ThrusterFlameController> _thrusters;
    private Vector2 _thrusterVector;
    private IInputCommands _sprintButton;
    private bool _isDreamLanternFocused;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(SpeedController));
        ModController.s_instance.OnConfigure += ApplyChanges;
    }

    private void Update()
    {
        if (_characterController == null) return;

        bool rollInputChanged = OWInput.IsNewlyPressed(InputLibrary.rollMode) || OWInput.IsNewlyReleased(InputLibrary.rollMode);
        bool downInputChanged = OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown);
        bool upInputChanged = OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyReleased(InputLibrary.thrustUp);
        bool boostedInMidair = OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded();

        // if holding a dream lantern, find out if the focus has changed since last frame
        DreamLanternItem heldLantern = _characterController._heldLanternItem;
        bool dreamLanternFocusChanged = heldLantern ? heldLantern._focusing != _isDreamLanternFocused : false;
        if (dreamLanternFocusChanged) _isDreamLanternFocused = heldLantern ? heldLantern._focusing : false;

        if (rollInputChanged || downInputChanged || upInputChanged || boostedInMidair || dreamLanternFocusChanged) UpdateSprinting();
    }

    private void LateUpdate()
    {
        if (_characterController == null) return;

        // get thruster vector IF the player is sprinting and the jetpack is visible. Otherwise, move towards zero
        _thrusterVector = Vector2.MoveTowards(_thrusterVector, IsSprinting && _playerVFX.activeSelf ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero, Time.deltaTime * 5);
        Vector2 flameVector = _thrusterVector;

        // adjust vector based on sprinting and strafe speed
        flameVector.x *= (ModController.s_instance.SprintStrafeSpeed / ModController.s_instance.StrafeSpeed) - 1;
        if (flameVector.y < 0f)
        {
            flameVector.y *= (ModController.s_instance.SprintStrafeSpeed / ModController.s_instance.StrafeSpeed) - 1;
        }
        else
        {
            flameVector.y *= (ModController.s_instance.SprintSpeed / ModController.s_instance.DefaultSpeed) - 1;
        }

        // clamp the vector so it doesn't become too big
        flameVector.x = Mathf.Clamp(flameVector.x, -20, 20);
        flameVector.y = Mathf.Clamp(flameVector.y, -20, 20);

        // update thruster sound, as long as it's not being set by the actual audio controller
        bool underwater = _jetpackAudio._underwater;
        bool overrideAudio = _jetpackAudio.isActiveAndEnabled == false;
        if (overrideAudio)
        {
            float soundVolume = flameVector.magnitude;
            float soundPan = -flameVector.x * 0.4f;
            bool hasFuel = _jetpackAudio._playerResources.GetFuel() > 0f;
            _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._translationalSource, soundVolume, soundPan, !underwater && hasFuel);
            _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._underwaterSource, soundVolume, soundPan, underwater);
            _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._oxygenSource, soundVolume, soundPan, !underwater && !hasFuel);
        }

        // update thruster visuals as long as their controllers are inactive
        for (int i = 0; i < _thrusters.Count; i++)
        {
            if (_thrusters[i].isActiveAndEnabled) break;

            switch (_thrusters[i]._thruster)
            {
                case Thruster.Forward_LeftThruster:
                    SetThrusterScale(_thrusters[i], flameVector.y);
                    break;
                case Thruster.Forward_RightThruster:
                    SetThrusterScale(_thrusters[i], flameVector.y);
                    break;
                case Thruster.Left_Thruster:
                    SetThrusterScale(_thrusters[i], -flameVector.x);
                    break;
                case Thruster.Right_Thruster:
                    SetThrusterScale(_thrusters[i], flameVector.x);
                    break;
                case Thruster.Backward_LeftThruster:
                    SetThrusterScale(_thrusters[i], -flameVector.y);
                    break;
                case Thruster.Backward_RightThruster:
                    SetThrusterScale(_thrusters[i], -flameVector.y);
                    break;
            }
        }
    }

    private void ApplyChanges()
    {
        if (_characterController == null) return;

        // Change built-in character attributes
        _characterController._runSpeed = ModController.s_instance.DefaultSpeed;
        _characterController._strafeSpeed = ModController.s_instance.StrafeSpeed;
        _characterController._walkSpeed = ModController.s_instance.WalkSpeed;
        _characterController._airSpeed = ModController.s_instance.AirSpeed;
        _characterController._airAcceleration = ModController.s_instance.AirAccel;

        _sprintButton = (ModController.s_instance.SprintButtonMode == "Down Thrust") ? InputLibrary.thrustDown : InputLibrary.thrustUp;

        UpdateSprinting();
    }

    private void UpdateSprinting()
    {
        bool isSprintAllowed = ModController.s_instance.SprintMode == "Always" || (ModController.s_instance.SprintMode == "When Suited" && PlayerState.IsWearingSuit());
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        bool isWalking = _isDreamLanternFocused || (OWInput.IsPressed(InputLibrary.rollMode) && _characterController._heldLanternItem == null);
        bool wasSprinting = _isSprinting;

        if (isSprintAllowed && isOnValidGround && !isWalking && OWInput.IsPressed(_sprintButton) && (IsSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0))
        {
            IsSprinting = true;
            _characterController._runSpeed = ModController.s_instance.SprintSpeed;
            _characterController._strafeSpeed = ModController.s_instance.SprintStrafeSpeed;
        }
        else
        {
            IsSprinting = false;
            _characterController._runSpeed = ModController.s_instance.DefaultSpeed;
            _characterController._strafeSpeed = ModController.s_instance.StrafeSpeed;
        }

        // log it
        if (_isSprinting != wasSprinting)
        {
            ModController.s_instance.DebugLog($"{(_isSprinting ? "Started" : "Stopped")} sprinting");
        }
    }

    private void SetThrusterScale(ThrusterFlameController thruster, float thrusterScale)
    {
        if (thruster._underwater) thrusterScale = 0f;

        // reset scale spring if it's rly small so it doesn't bounce back up
        if (thruster._currentScale <= 0.001f)
        {
            thruster._currentScale = 0f;
            thruster._scaleSpring.ResetVelocity();
        }

        thruster._currentScale = thruster._scaleSpring.Update(thruster._currentScale, thrusterScale, Time.deltaTime);
        thruster.transform.localScale = Vector3.one * thruster._currentScale;
        thruster._light.range = thruster._baseLightRadius * thruster._currentScale;
        thruster._thrusterRenderer.enabled = thruster._currentScale > 0f;
        thruster._light.enabled = thruster._currentScale > 0f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart()
    {
        s_instance._characterController = Locator.GetPlayerController();
        s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
        s_instance._jetpackAudio = FindObjectOfType<JetpackThrusterAudio>();
        s_instance._playerVFX = s_instance._characterController.GetComponentInChildren<PlayerParticlesController>(includeInactive: true).gameObject;
        s_instance._thrusters = new(s_instance._characterController.gameObject.GetComponentsInChildren<ThrusterFlameController>(includeInactive: true));
        s_instance._thrusterVector = Vector2.zero;
        s_instance._characterController.OnBecomeGrounded += s_instance.UpdateSprinting; // maybe make this optional in the config?
        s_instance._isDreamLanternFocused = false;

        s_instance.ApplyChanges();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    private static void OnGetJetpackInput(ref Vector3 __result)
    {
        if (s_instance.IsSprinting && __result.y != 0f)
        {
            __result.y = 0f;
            s_instance._jetpackModel._boostActivated = false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
    private static bool CharacterControllerUpdate(PlayerCharacterController __instance)
    {
        if (!__instance._isAlignedToForce && !__instance._isZeroGMovementEnabled) return false;

        if ((OWInput.GetValue(InputLibrary.thrustUp, InputMode.All) == 0f) || (s_instance._sprintButton == InputLibrary.thrustUp && s_instance.IsSprinting))
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
    private static bool IsBoosterAllowed(ref bool __result, PlayerResources __instance)
    {
        __result = !PlayerState.InZeroG() && !Locator.GetPlayerSuit().IsTrainingSuit() && !__instance._cameraFluidDetector.InFluidType(FluidVolume.Type.WATER) && __instance._currentFuel > 0f && !s_instance.IsSprinting;
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
            __instance._footstepAudio.PlayOneShot(audioType, 1.4f * s_instance._characterController.GetRelativeGroundVelocity().magnitude / 6);
        }
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
    private static bool OverrideMaxRunSpeed(ref float maxSpeedX, ref float maxSpeedZ, DreamLanternItem __instance)
    {
        float lerpPosition = 1f - __instance._lanternController.GetFocus();
        lerpPosition *= lerpPosition;
        maxSpeedX = Mathf.Lerp(ModController.s_instance.DreamLanternSpeed, maxSpeedX, lerpPosition);
        maxSpeedZ = Mathf.Lerp(ModController.s_instance.DreamLanternSpeed, maxSpeedZ, lerpPosition);
        return false;
    }
}
