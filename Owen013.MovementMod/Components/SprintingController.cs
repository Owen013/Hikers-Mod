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
        bool isTryingToMove = OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f;

        bool isSprintKeyboardKeyPressed = Keyboard.current != null && Keyboard.current[Config.SprintKey].isPressed;
        bool isSprintGamepadButtonPressed = Gamepad.current != null && Gamepad.current[Config.SprintGamepadButton].isPressed;
        bool isSprintButtonHeld = isSprintKeyboardKeyPressed || isSprintGamepadButtonPressed;

        bool wasSprintKeyboardKeyJustPressed = Keyboard.current != null && Keyboard.current[Config.SprintKey].wasPressedThisFrame;
        bool wasSprintGamepadButtonJustPressed = Gamepad.current != null && Gamepad.current[Config.SprintGamepadButton].wasPressedThisFrame;
        bool wasSprintButtonJustPressed = wasSprintKeyboardKeyJustPressed || wasSprintGamepadButtonJustPressed;

        bool isStartingVerticalThrust = OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyPressed(InputLibrary.thrustDown);
        bool wasHoldingVerticalThrust = !isStartingVerticalThrust && (OWInput.IsPressed(InputLibrary.thrustUp) || OWInput.IsPressed(InputLibrary.thrustDown));

        bool canBoost = PlayerState.IsWearingSuit() && !Locator.GetPlayerSuit().IsTrainingSuit();
        bool isStartingBoost = canBoost && OWInput.IsPressed(InputLibrary.thrustUp) && OWInput.IsNewlyPressed(InputLibrary.boost);

        bool canStartSprint = isOnValidGround && isTryingToMove && wasSprintButtonJustPressed && !wasHoldingVerticalThrust;
        bool canMaintainSprint = isTryingToMove && !isStartingVerticalThrust && (isOnValidGround || !isStartingBoost);
        if (Config.IsHoldToSprintEnabled)
        {
            canMaintainSprint = isSprintButtonHeld && canMaintainSprint;
        }

        if (canStartSprint || (IsSprinting && canMaintainSprint))
        {
            if (!IsSprinting)
            {
                StartSprinting();
            }
        }
        else
        {
            if (IsSprinting)
            {
                StopSprinting();
            }
        }
    }

    private void OnConfigure()
    {
        enabled = Config.IsSprintingEnabled;
    }

    private void Awake()
    {
        Instance = this;
        _playerController = Locator.GetPlayerController();

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