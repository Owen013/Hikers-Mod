using UnityEngine;

namespace HikersMod.Components;

public class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprintModeActive { get; private set; }

    private PlayerCharacterController _characterController;

    private IInputCommands _sprintButton;

    private float _lastSprintTime;

    private float _staminaSecondsLeft;

    public bool IsSprinting()
    {
        if (!_characterController.IsGrounded() || !IsSprintModeActive) return false;

        Vector2 inputVector = OWInput.GetAxisValue(InputLibrary.moveXZ);
        Vector3 groundVelocity = _characterController.GetRelativeGroundVelocity();
        groundVelocity.y = 0f;

        // get minimum sprint speed by finding the max speed the player could be going without sprinting
        Vector2 normalizedInputVector = inputVector.normalized;
        float minSprintSpeed = new Vector3(Config.StrafeSpeed * normalizedInputVector.x, 0f, (inputVector.y > 0f ? Config.RunSpeed : Config.StrafeSpeed) * normalizedInputVector.y).magnitude;

        return inputVector.magnitude > 1f / Config.SprintMultiplier && groundVelocity.magnitude > minSprintSpeed;
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

        if (IsSprintModeActive && (!Config.IsStaminaEnabled || _staminaSecondsLeft > 0f))
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
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();

        if (Config.IsSprintingEnabled && isOnValidGround && OWInput.IsPressed(_sprintButton) && (IsSprintModeActive || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
        {
            IsSprintModeActive = true;
        }
        else
        {
            IsSprintModeActive = false;
        }
    }

    private void UpdateStamina()
    {
        if (Config.IsStaminaEnabled && IsSprinting())
        {
            _staminaSecondsLeft -= Time.deltaTime;
            _lastSprintTime = Time.time;
        }
        else if (Time.time - _lastSprintTime >= 1f)
        {
            _staminaSecondsLeft += Time.deltaTime * Config.StaminaRecoveryRate;
        }

        // make sure stamina seconds left is within possible range
        _staminaSecondsLeft = Mathf.Clamp(_staminaSecondsLeft, 0f, Config.StaminaSeconds);
    }
}