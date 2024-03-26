using UnityEngine;

namespace HikersMod.Components;

public class SprintController : MonoBehaviour
{
    public static SprintController Instance { get; private set; }
    public bool IsSprintActive { get; private set; }

    private PlayerCharacterController _characterController;
    private IInputCommands _sprintButton;
    private float _staminaSecondsLeft;
    private float _lastSprintTime;

    public bool IsSprinting()
    {
        if (!_characterController.IsGrounded()) return false;

        Vector3 groundVelocity = _characterController.GetRelativeGroundVelocity();
        groundVelocity.y = 0f;

        return IsSprintActive && HasStamina() && OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f && groundVelocity.magnitude > Config.RunSpeed;
    }

    public bool HasStamina()
    {
        return _staminaSecondsLeft > 0f || Config.IsStaminaEnabled;
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

        if (IsSprintActive && HasStamina())
        {
            _characterController._runSpeed = Config.RunSpeed * Config.SprintMultiplier;
            _characterController._strafeSpeed = Config.StrafeSpeed * Config.SprintMultiplier;
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
        bool isSprintAllowed = Config.IsSprintingEnabled;
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        bool wasSprintActive = IsSprintActive;

        if (isSprintAllowed && isOnValidGround && OWInput.IsPressed(_sprintButton) && (wasSprintActive || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
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
        if (IsSprinting())
        {
            _staminaSecondsLeft -= Time.deltaTime;
            _lastSprintTime = Time.time;
            if (_staminaSecondsLeft <= 0f)
            {
                _staminaSecondsLeft = 0f;
            }
        }
        else if (Time.time - _lastSprintTime >= 1f)
        {
            _staminaSecondsLeft += Time.deltaTime * Config.StaminaRecoveryRate;
            if (_staminaSecondsLeft >= Config.StaminaSeconds)
            {
                _staminaSecondsLeft = Config.StaminaSeconds;
            }
        }
    }
}