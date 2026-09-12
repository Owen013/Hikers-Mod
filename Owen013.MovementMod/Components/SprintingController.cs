using UnityEngine;
using UnityEngine.InputSystem;

namespace HikersMod.Components;

public class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprinting { get; private set; }

    private PlayerCharacterController _playerController;

    private void StartSprinting()
    {
        IsSprinting = true;
        _playerController._runSpeed = Config.RunSpeed * Config.SprintMultiplier;
        _playerController._strafeSpeed = Config.StrafeSpeed * Config.SprintMultiplier;
    }

    private void StopSprinting()
    {
        IsSprinting = false;
        _playerController._runSpeed = Config.RunSpeed;
        _playerController._strafeSpeed = Config.StrafeSpeed;
    }

    private void UpdateSprinting()
    {
        bool isOnValidGround = _playerController.IsGrounded() && !_playerController.IsSlidingOnIce();
        bool isSprintKeyboardKeyPressed = Keyboard.current != null && Keyboard.current[Config.SprintKey].isPressed;
        bool isSprintGamepadButtonPressed = Gamepad.current != null && Gamepad.current[Config.SprintGamepadButton].isPressed;
        bool isSprintButtonPressed = isSprintKeyboardKeyPressed || isSprintGamepadButtonPressed;
        bool isTryingToMove = OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f;
        bool canStartSprint = isOnValidGround && isTryingToMove && isSprintButtonPressed;
        bool canMaintainSprint;
        if (Config.IsHoldToSprintEnabled)
        {
            bool isTryingToBoost = OWInput.IsPressed(InputLibrary.thrustUp) && OWInput.IsPressed(InputLibrary.boost);
            canMaintainSprint = isTryingToMove && isSprintButtonPressed && !isTryingToBoost;
        }
        else
        {
            bool isUsingVerticalThrust = OWInput.IsPressed(InputLibrary.thrustUp) || OWInput.IsPressed(InputLibrary.thrustDown);
            canMaintainSprint = isTryingToMove && (isOnValidGround || !isUsingVerticalThrust);
        }

        if (enabled && canStartSprint || (IsSprinting && canMaintainSprint))
        {
            StartSprinting();
        }
        else
        {
            StopSprinting();
        }
    }

    private void OnConfigure()
    {
        enabled = Config.IsSprintingEnabled;

        UpdateSprinting();
    }

    private void Awake()
    {
        Instance = this;
        _playerController = GetComponent<PlayerCharacterController>();

        _playerController.OnBecomeGrounded += () =>
        {
            if (Config.ShouldSprintOnLanding)
            {
                UpdateSprinting();
            }
        };

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void Update()
    {
        UpdateSprinting();
    }

    private void OnDisable()
    {
        StopSprinting();
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}