using UnityEngine;

namespace HikersMod.Components;

public class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprintModeActive { get; private set; }

    public float StaminaSecondsLeft { get; private set; }

    private PlayerCharacterController _characterController;

    private IInputCommands _sprintButton;

    private float _lastSprintTime;

    public bool IsSprinting()
    {
        if (!_characterController.IsGrounded() || !IsSprintModeActive) return false;

        Vector2 inputVector = OWInput.GetAxisValue(InputLibrary.moveXZ);
        Vector3 groundVelocity = _characterController.GetRelativeGroundVelocity();
        groundVelocity.y = 0f;

        // get minimum sprint speed by finding the max speed the player could be going without sprinting
        Vector2 normalizedInputVector = inputVector.normalized;
        float minSprintSpeed = new Vector3(ModMain.StrafeSpeed * normalizedInputVector.x, 0f, (inputVector.y > 0f ? ModMain.RunSpeed : ModMain.StrafeSpeed) * normalizedInputVector.y).magnitude;

        return inputVector.magnitude > 1f / ModMain.SprintMultiplier && groundVelocity.magnitude > minSprintSpeed;
    }

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();
        StaminaSecondsLeft = ModMain.StaminaSeconds;

        _characterController.OnBecomeGrounded += () =>
        {
            if (ModMain.ShouldSprintOnLanding)
            {
                UpdateSprinting();
            }
        };

        ModMain.OnConfigure += ApplyChanges;
        ApplyChanges();
    }

    private void OnDestroy()
    {
        ModMain.OnConfigure -= ApplyChanges;
    }

    private void ApplyChanges()
    {
        // Change built-in character attributes
        _characterController._runSpeed = ModMain.RunSpeed;
        _characterController._strafeSpeed = ModMain.StrafeSpeed;
        _characterController._walkSpeed = ModMain.WalkSpeed;
        _characterController._airSpeed = ModMain.AirSpeed;
        _characterController._airAcceleration = ModMain.AirAccel;
        _sprintButton = ModMain.SprintButton == "Up Thrust" ? InputLibrary.thrustUp : InputLibrary.thrustDown;

        UpdateSprinting();
    }

    private void Update()
    {
        bool hasVerticalThrustChanged = OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyReleased(InputLibrary.thrustUp) || OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown);

        if (hasVerticalThrustChanged || (OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded()))
        {
            UpdateSprinting();
        }

        if (IsSprintModeActive && (!ModMain.IsStaminaEnabled || StaminaSecondsLeft > 0f))
        {
            _characterController._runSpeed = ModMain.RunSpeed * ModMain.SprintMultiplier;
            _characterController._strafeSpeed = ModMain.StrafeSpeed * ModMain.SprintMultiplier;
        }
        else
        {
            _characterController._runSpeed = ModMain.RunSpeed;
            _characterController._strafeSpeed = ModMain.StrafeSpeed;
        }

        UpdateStamina();
    }

    private void UpdateSprinting()
    {
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();

        if (ModMain.IsSprintingEnabled && isOnValidGround && OWInput.IsPressed(_sprintButton) && (IsSprintModeActive || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
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
        if (ModMain.IsStaminaEnabled && IsSprinting())
        {
            StaminaSecondsLeft -= Time.deltaTime;
            _lastSprintTime = Time.time;
        }
        else if (Time.time - _lastSprintTime >= 1f)
        {
            StaminaSecondsLeft += Time.deltaTime * ModMain.StaminaRecoveryRate;
        }

        // make sure stamina seconds left is within possible range
        StaminaSecondsLeft = Mathf.Clamp(StaminaSecondsLeft, 0f, ModMain.StaminaSeconds);
    }
}