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
    private bool _isUsingStamina;
    private bool _isTired;
    private float _lastStaminaUseTime;
    private float _staminaSecondsLeft;

    private void Awake()
    {
        Instance = this;

        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackAudio = GetComponentInChildren<JetpackThrusterAudio>();
        _thrusters = GetComponentsInChildren<ThrusterFlameController>(includeInactive: true);
        _playerSuit = GetComponentInChildren<PlayerAnimController>().transform.Find("Traveller_Mesh_v01:Traveller_Geo").gameObject;
        _playerJetpack = _playerSuit.transform.Find("Traveller_Mesh_v01:Props_HEA_Jetpack").gameObject;
        _sprintButton = Config.sprintButton == "Up Thrust" ? InputLibrary.thrustUp : InputLibrary.thrustDown;
        _thrusterVector = Vector2.zero;
        _staminaSecondsLeft = Config.staminaSeconds;

        _characterController.OnBecomeGrounded += () =>
        {
            if (Config.shouldSprintOnLanding)
            {
                UpdateSprinting();
            }
        };

        Main.Instance.OnConfigure += ApplyChanges;
        ApplyChanges();
    }

    private void OnDestroy()
    {
        Main.Instance.OnConfigure -= ApplyChanges;
    }

    private void ApplyChanges()
    {
        // Change built-in character attributes
        _characterController._runSpeed = Config.runSpeed;
        _characterController._strafeSpeed = Config.strafeSpeed;
        _characterController._walkSpeed = Config.walkSpeed;
        _characterController._airSpeed = Config.airSpeed;
        _characterController._airAcceleration = Config.airAccel;

        UpdateSprinting();
    }

    private void Update()
    {
        bool justBecameTired = false;
        float currentMoveSpeed = _characterController.GetRelativeGroundVelocity().magnitude;
        if (currentMoveSpeed <= Config.runSpeed)
        {
            
        }
        if (IsSprinting)
        {
            _staminaSecondsLeft -= Time.deltaTime * Mathf.Clamp01((currentMoveSpeed - Config.runSpeed) / Mathf.Max(1f, Config.runSpeed * Config.sprintMultiplier));
            if (_staminaSecondsLeft <= 0f)
            {
                _staminaSecondsLeft = 0f;
                _isTired = true;
                justBecameTired = true;
            }
        }
        else if (Time.time - _lastStaminaUseTime > 1f)
        {
            _staminaSecondsLeft += Config.staminaRecoveryRate * Time.deltaTime;
            if (_staminaSecondsLeft > Config.staminaSeconds)
            {
                _staminaSecondsLeft = Config.staminaSeconds;
                _isTired = false;
            }
        };

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

        if (justBecameTired || hasRollInputChanged || hasThrustInputChanged || hasPressedBoostInMidair || hasDreamLanternFocusChanged) UpdateSprinting();
    }

    private void UpdateSprinting()
    {
        bool isSprintAllowed = Config.isSprintingEnabled && !_isTired;
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        bool isWalking = _isDreamLanternFocused || (OWInput.IsPressed(InputLibrary.rollMode) && _characterController._heldLanternItem == null);
        bool wasSprinting = IsSprinting;

        if (isSprintAllowed && isOnValidGround && !isWalking && OWInput.IsPressed(_sprintButton) && (wasSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
        {
            IsSprinting = true;
            _characterController._runSpeed = Config.runSpeed * Config.sprintMultiplier;
            _characterController._strafeSpeed = Config.strafeSpeed * Config.sprintMultiplier;
        }
        else
        {
            IsSprinting = false;
            _characterController._runSpeed = Config.runSpeed;
            _characterController._strafeSpeed = Config.strafeSpeed;
            if (!_isUsingStamina)
            {

            }
        }

        // log it
        if (IsSprinting != wasSprinting)
        {
            Main.WriteLine($"[{nameof(SpeedController)}] {(IsSprinting ? "Started" : "Stopped")} sprinting", MessageType.Debug);
        }
    }

    private void LateUpdate()
    {
        bool jetpackVisible = _playerSuit.activeSelf && _playerJetpack.activeSelf;

        // get thruster vector IF the player is sprinting and the jetpack is visible. Otherwise, move towards zero
        _thrusterVector = Vector2.MoveTowards(_thrusterVector, IsSprinting && jetpackVisible && Config.isSprintEffectEnabled ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero, Time.deltaTime * 5);
        Vector2 flameVector = _thrusterVector;

        // clamp the vector so it doesn't become too big
        flameVector.x = Mathf.Clamp(flameVector.x, -20, 20);
        flameVector.y = Mathf.Clamp(flameVector.y, -20, 20);

        // update thruster sound, as long as it's not being set by the actual audio controller
        if (_jetpackAudio.isActiveAndEnabled == false)
        {
            float soundVolume = flameVector.magnitude;
            float soundPan = -flameVector.x * 0.4f;
            bool hasFuel = _jetpackAudio._playerResources.GetFuel() > 0f;
            bool isUnderwater = _jetpackAudio._underwater;
            _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._translationalSource, soundVolume, soundPan, !isUnderwater && hasFuel);
            _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._underwaterSource, soundVolume, soundPan, isUnderwater);
            _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._oxygenSource, soundVolume, soundPan, !isUnderwater && !hasFuel);
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

    private static void SetThrusterScale(ThrusterFlameController thruster, float thrusterScale)
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