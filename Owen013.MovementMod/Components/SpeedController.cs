using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace HikersMod.Components;

public class SpeedController : MonoBehaviour
{
    public static SpeedController Instance;
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

    public bool IsSprinting()
    {
        return _isSprinting;
    }

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(typeof(SpeedController));
        Main.Instance.OnConfigure += ApplyChanges;
    }

    private void Start()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackModel = FindObjectOfType<JetpackThrusterModel>();
        _jetpackAudio = FindObjectOfType<JetpackThrusterAudio>();
        _playerSuit = GameObject.Find("Player_Body/Traveller_HEA_Player_v2/Traveller_Mesh_v01:Traveller_Geo");
        _playerJetpack = GameObject.Find("Player_Body/Traveller_HEA_Player_v2/Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:Props_HEA_Jetpack");
        _thrusters = new(GetComponentsInChildren<ThrusterFlameController>(includeInactive: true));
        _thrusterVector = Vector2.zero;
        _isDreamLanternFocused = false;

        _characterController.OnBecomeGrounded += () =>
        {
            if (Main.Instance.ShouldSprintOnLanding)
            {
                UpdateSprinting();
            }
        };

        ApplyChanges();
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
        _thrusterVector = Vector2.MoveTowards(_thrusterVector, _isSprinting && jetpackVisible && Main.Instance.IsSprintEffectEnabled ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero, Time.deltaTime * 5);
        Vector2 flameVector = _thrusterVector;

        // adjust vector based on sprinting and strafe speed
        flameVector.x *= (Main.Instance.SprintStrafeSpeed / Main.Instance.StrafeSpeed) - 1;
        if (flameVector.y < 0f)
        {
            flameVector.y *= (Main.Instance.SprintStrafeSpeed / Main.Instance.StrafeSpeed) - 1;
        }
        else
        {
            flameVector.y *= (Main.Instance.SprintSpeed / Main.Instance.DefaultSpeed) - 1;
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
        _characterController._runSpeed = Main.Instance.DefaultSpeed;
        _characterController._strafeSpeed = Main.Instance.StrafeSpeed;
        _characterController._walkSpeed = Main.Instance.WalkSpeed;
        _characterController._airSpeed = Main.Instance.AirSpeed;
        _characterController._airAcceleration = Main.Instance.AirAccel;

        _sprintButton = (Main.Instance.SprintButtonMode == "Down Thrust") ? InputLibrary.thrustDown : InputLibrary.thrustUp;

        UpdateSprinting();
    }

    private void UpdateSprinting()
    {
        bool isSprintAllowed = Main.Instance.SprintMode == "Always" || (Main.Instance.SprintMode == "When Suited" && PlayerState.IsWearingSuit());
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        bool isWalking = _isDreamLanternFocused || (OWInput.IsPressed(InputLibrary.rollMode) && _characterController._heldLanternItem == null);
        bool wasSprinting = _isSprinting;

        if (isSprintAllowed && isOnValidGround && !isWalking && OWInput.IsPressed(_sprintButton) && (_isSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0))
        {
            _isSprinting = true;
            _characterController._runSpeed = Main.Instance.SprintSpeed;
            _characterController._strafeSpeed = Main.Instance.SprintStrafeSpeed;
        }
        else
        {
            _isSprinting = false;
            _characterController._runSpeed = Main.Instance.DefaultSpeed;
            _characterController._strafeSpeed = Main.Instance.StrafeSpeed;
        }

        // log it
        if (_isSprinting != wasSprinting)
        {
            Main.Instance.DebugLog($"{(_isSprinting ? "Started" : "Stopped")} sprinting");
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
    [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    private static void OnGetJetpackInput(ref Vector3 __result)
    {
        if (Components.SpeedController.Instance.IsSprinting() == true && __result.y != 0f)
        {
            __result.y = 0f;
            Instance._jetpackModel._boostActivated = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
    private static void CharacterControllerUpdate(PlayerCharacterController __instance)
    {
        if (Components.SpeedController.Instance.IsSprinting() == true || !Instance._characterController._isWearingSuit)
        {
            __instance.UpdateJumpInput();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.IsBoosterAllowed))]
    private static void IsBoosterAllowed(ref bool __result, PlayerResources __instance)
    {
        // prevents player from jumping higher when sprinting
        if (Components.SpeedController.Instance.IsSprinting() == true) __result = false;
    }
}
