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
    private GameObject _playerSuit;
    private GameObject _playerJetpack;
    private List<ThrusterFlameController> _thrusters;
    private Vector2 _thrusterVector;
    private IInputCommands _sprintButton;
    private bool _isSprinting;
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

        bool jetpackVisible = _playerSuit.activeSelf && _playerJetpack.activeSelf;

        // get thruster vector IF the player is sprinting and the jetpack is visible. Otherwise, move towards zero
        _thrusterVector = Vector2.MoveTowards(_thrusterVector, _isSprinting && jetpackVisible && ModController.s_instance.isSprintEffectEnabled ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero, Time.deltaTime * 5);
        Vector2 flameVector = _thrusterVector;

        // adjust vector based on sprinting and strafe speed
        flameVector.x *= (ModController.s_instance.sprintStrafeSpeed / ModController.s_instance.strafeSpeed) - 1;
        if (flameVector.y < 0f)
        {
            flameVector.y *= (ModController.s_instance.sprintStrafeSpeed / ModController.s_instance.strafeSpeed) - 1;
        }
        else
        {
            flameVector.y *= (ModController.s_instance.sprintSpeed / ModController.s_instance.defaultSpeed) - 1;
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
        _characterController._runSpeed = ModController.s_instance.defaultSpeed;
        _characterController._strafeSpeed = ModController.s_instance.strafeSpeed;
        _characterController._walkSpeed = ModController.s_instance.walkSpeed;
        _characterController._airSpeed = ModController.s_instance.airSpeed;
        _characterController._airAcceleration = ModController.s_instance.airAccel;

        _sprintButton = (ModController.s_instance.sprintButtonMode == "Down Thrust") ? InputLibrary.thrustDown : InputLibrary.thrustUp;

        UpdateSprinting();
    }

    private void UpdateSprinting()
    {
        bool isSprintAllowed = ModController.s_instance.sprintMode == "Always" || (ModController.s_instance.sprintMode == "When Suited" && PlayerState.IsWearingSuit());
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        bool isWalking = _isDreamLanternFocused || (OWInput.IsPressed(InputLibrary.rollMode) && _characterController._heldLanternItem == null);
        bool wasSprinting = _isSprinting;

        if (isSprintAllowed && isOnValidGround && !isWalking && OWInput.IsPressed(_sprintButton) && (_isSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0))
        {
            _isSprinting = true;
            _characterController._runSpeed = ModController.s_instance.sprintSpeed;
            _characterController._strafeSpeed = ModController.s_instance.sprintStrafeSpeed;
        }
        else
        {
            _isSprinting = false;
            _characterController._runSpeed = ModController.s_instance.defaultSpeed;
            _characterController._strafeSpeed = ModController.s_instance.strafeSpeed;
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

    public string GetMoveSpeed()
    {
        if (_isSprinting) return "sprinting";
        else if (_isDreamLanternFocused) return "dreamLanternFocused";
        else if (OWInput.IsPressed(InputLibrary.rollMode) && _characterController._heldLanternItem == null) return "walking";
        else return "normal";
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart()
    {
        s_instance._characterController = Locator.GetPlayerController();
        s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
        s_instance._jetpackAudio = FindObjectOfType<JetpackThrusterAudio>();
        s_instance._playerSuit = GameObject.Find("Player_Body/Traveller_HEA_Player_v2/Traveller_Mesh_v01:Traveller_Geo");
        s_instance._playerJetpack = GameObject.Find("Player_Body/Traveller_HEA_Player_v2/Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:Props_HEA_Jetpack");
        s_instance._thrusters = new(s_instance._characterController.gameObject.GetComponentsInChildren<ThrusterFlameController>(includeInactive: true));
        s_instance._thrusterVector = Vector2.zero;
        s_instance._isDreamLanternFocused = false;

        s_instance._characterController.OnBecomeGrounded += () =>
        {
            if (ModController.s_instance.sprintOnLanding)
            {
                s_instance.UpdateSprinting();
            }
        };

        s_instance.ApplyChanges();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    private static void OnGetJetpackInput(ref Vector3 __result)
    {
        if (s_instance._isSprinting && __result.y != 0f)
        {
            __result.y = 0f;
            s_instance._jetpackModel._boostActivated = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
    private static void CharacterControllerUpdate(PlayerCharacterController __instance)
    {
        if (s_instance._isSprinting || !s_instance._characterController._isWearingSuit)
        {
            __instance.UpdateJumpInput();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.IsBoosterAllowed))]
    private static void IsBoosterAllowed(ref bool __result, PlayerResources __instance)
    {
        // prevents player from jumping higher when sprinting
        if (s_instance._isSprinting) __result = false;
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
        maxSpeedX = Mathf.Lerp(ModController.s_instance.dreamLanternSpeed, maxSpeedX, lerpPosition);
        maxSpeedZ = Mathf.Lerp(ModController.s_instance.dreamLanternSpeed, maxSpeedZ, lerpPosition);
        return false;
    }
}
