using UnityEngine;

namespace HikersMod.Components;

public class SprintController : MonoBehaviour
{
    public static SprintController Instance { get; private set; }
    public bool IsSprintActive { get; private set; }

    private PlayerCharacterController _characterController;
    private IInputCommands _sprintButton;
    private bool _isTired;
    private float _staminaSecondsLeft;
    private float _lastSprintTime;

    public bool IsSprinting()
    {
        if (!_characterController.IsGrounded() || !IsSprintActive || _isTired) return false;

        Vector3 groundVelocity = _characterController.GetRelativeGroundVelocity();
        groundVelocity.y = 0f;
        return OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f && groundVelocity.magnitude > Config.RunSpeed;
    }

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();
        _staminaSecondsLeft = Config.StaminaSeconds;

        _characterController.OnBecomeGrounded += () =>
        {
            if (Config.ShouldSprintOnLanding)
            {
                UpdateSprinting();
            }
        };

        Config.OnConfigure += ApplyChanges;
        ApplyChanges();
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= ApplyChanges;
    }

    private void ApplyChanges()
    {
        // Change built-in character attributes
        _characterController._runSpeed = Config.RunSpeed;
        _characterController._strafeSpeed = Config.StrafeSpeed;
        _characterController._walkSpeed = Config.WalkSpeed;
        _characterController._airSpeed = Config.AirSpeed;
        _characterController._airAcceleration = Config.AirAccel;
        _sprintButton = Config.SprintButton == "Up Thrust" ? InputLibrary.thrustUp : InputLibrary.thrustDown;

        UpdateSprinting();
    }

    private void Update()
    {
        bool hasVerticalThrustChanged = OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyReleased(InputLibrary.thrustUp) || OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown);

        if (hasVerticalThrustChanged || (OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded()))
        {
            UpdateSprinting();
        }

        if (_isTired)
        {
            _characterController._runSpeed = Config.RunSpeed * Config.TiredMultiplier;
            _characterController._strafeSpeed = Config.StrafeSpeed * Config.TiredMultiplier;
        }
        else if (IsSprintActive)
        {
            _characterController._runSpeed = Config.RunSpeed + (Config.RunSpeed * Config.SprintMultiplier - Config.RunSpeed) * (0.5f + 0.5f * _staminaSecondsLeft / Config.StaminaSeconds);
            _characterController._strafeSpeed = Config.StrafeSpeed + (Config.StrafeSpeed * Config.SprintMultiplier - Config.StrafeSpeed) * (0.5f + 0.5f * _staminaSecondsLeft / Config.StaminaSeconds);
        }
        else
        {
            _characterController._runSpeed = Config.RunSpeed;
            _characterController._strafeSpeed = Config.StrafeSpeed;
        }

        UpdateStamina();
    }

    private void UpdateSprinting()
    {
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();

        if (Config.IsSprintingEnabled && isOnValidGround && OWInput.IsPressed(_sprintButton) && (IsSprintActive || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
        {
            IsSprintActive = true;
        }
        else
        {
            IsSprintActive = false;
        }
    }

    private void UpdateStamina()
    {
        if (!Config.IsStaminaEnabled)
        {
            _isTired = false;
            _staminaSecondsLeft = Config.StaminaSeconds;
        }
        else if (IsSprinting())
        {
            _staminaSecondsLeft -= Time.deltaTime;
            _lastSprintTime = Time.time;
            if (_staminaSecondsLeft <= 0f)
            {
                _staminaSecondsLeft = 0f;
                _isTired = true;
            }
        }
        else if (Time.time - _lastSprintTime >= 1f)
        {
            _staminaSecondsLeft += Time.deltaTime * Config.StaminaRecoveryRate;
            if (_staminaSecondsLeft >= Config.StaminaSeconds)
            {
                _staminaSecondsLeft = Config.StaminaSeconds;
                _isTired = false;
            }
        }
    }
}