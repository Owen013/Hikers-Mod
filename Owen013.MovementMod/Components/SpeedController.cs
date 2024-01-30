using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class SpeedController : MonoBehaviour
{
    public static SpeedController Instance { get; private set; }
    public bool IsSprinting { get; private set; }

    private PlayerCharacterController _characterController;
    private JetpackThrusterAudio _jetpackAudio;
    private ThrusterFlameController[] _thrusters;
    private GameObject _playerSuit;
    private GameObject _playerJetpack;
    private IInputCommands _sprintButton;
    private Vector2 _thrusterVector;
    private bool _isDreamLanternFocused;

    private void Awake()
    {
        Instance = this;

        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackAudio = GetComponentInChildren<JetpackThrusterAudio>();
        _thrusters = GetComponentsInChildren<ThrusterFlameController>(includeInactive: true);
        _playerSuit = GetComponentInChildren<PlayerAnimController>().transform.Find("Traveller_Mesh_v01:Traveller_Geo").gameObject;
        _playerJetpack = _playerSuit.transform.Find("Traveller_Mesh_v01:Props_HEA_Jetpack").gameObject;
        _sprintButton = Config.SprintButton == "Up Thrust" ? InputLibrary.thrustUp : InputLibrary.thrustDown;
        _thrusterVector = Vector2.zero;

        _characterController.OnBecomeGrounded += () =>
        {
            if (Config.ShouldSprintOnLanding)
            {
                UpdateSprinting();
            }
        };

        Main.Instance.OnConfigure += ApplyChanges;
        ApplyChanges();

        Main.Instance.Log($"{nameof(SpeedController)} added to {gameObject.name}", MessageType.Debug);
    }

    private void Update()
    {
        bool hasRollInputChanged = OWInput.IsNewlyPressed(InputLibrary.rollMode) || OWInput.IsNewlyReleased(InputLibrary.rollMode);
        bool hasThrustInputChanged = OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown) || OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyReleased(InputLibrary.thrustUp);
        bool hasPressedBoostInMidair = OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded();

        // if holding a dream lantern, find out if the focus has changed since last frame
        DreamLanternItem heldLantern = _characterController._heldLanternItem;
        bool hasDreamLanternFocusChanged = heldLantern != null && heldLantern._focusing != _isDreamLanternFocused;
        if (hasDreamLanternFocusChanged)
        {
            _isDreamLanternFocused = heldLantern._focusing;
        }

        if (hasRollInputChanged || hasThrustInputChanged || hasPressedBoostInMidair || hasDreamLanternFocusChanged) UpdateSprinting();
    }

    private void LateUpdate()
    {
        bool jetpackVisible = _playerSuit.activeSelf && _playerJetpack.activeSelf;

        // get thruster vector IF the player is sprinting and the jetpack is visible. Otherwise, move towards zero
        _thrusterVector = Vector2.MoveTowards(_thrusterVector, IsSprinting && jetpackVisible && Config.IsSprintEffectEnabled ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero, Time.deltaTime * 5);
        Vector2 flameVector = _thrusterVector;

        // adjust vector based on sprinting and strafe speed
        flameVector.x *= (Config.SprintStrafeSpeed / Config.StrafeSpeed) - 1;
        if (flameVector.y < 0f)
        {
            flameVector.y *= (Config.SprintStrafeSpeed / Config.StrafeSpeed) - 1;
        }
        else
        {
            flameVector.y *= (Config.SprintSpeed / Config.DefaultSpeed) - 1;
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
        for (int i = 0; i < _thrusters.Length; i++)
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
        // Change built-in character attributes
        _characterController._runSpeed = Config.DefaultSpeed;
        _characterController._strafeSpeed = Config.StrafeSpeed;
        _characterController._walkSpeed = Config.WalkSpeed;
        _characterController._airSpeed = Config.AirSpeed;
        _characterController._airAcceleration = Config.AirAccel;

        UpdateSprinting();
    }

    private void UpdateSprinting()
    {
        bool isSprintAllowed = Config.SprintMode == "Always" || (Config.SprintMode == "When Suited" && PlayerState.IsWearingSuit());
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        bool isWalking = _isDreamLanternFocused || (OWInput.IsPressed(InputLibrary.rollMode) && _characterController._heldLanternItem == null);
        bool wasSprinting = IsSprinting;

        if (isSprintAllowed && isOnValidGround && !isWalking && OWInput.IsPressed(_sprintButton) && (wasSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
        {
            IsSprinting = true;
            _characterController._runSpeed = Config.SprintSpeed;
            _characterController._strafeSpeed = Config.SprintStrafeSpeed;
        }
        else
        {
            IsSprinting = false;
            _characterController._runSpeed = Config.DefaultSpeed;
            _characterController._strafeSpeed = Config.StrafeSpeed;
        }

        // log it
        if (IsSprinting != wasSprinting)
        {
            Main.Instance.Log($"[{nameof(SpeedController)}] {(IsSprinting ? "Started" : "Stopped")} sprinting", MessageType.Debug);
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
}