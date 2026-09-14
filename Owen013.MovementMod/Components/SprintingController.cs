using UnityEngine;
using UnityEngine.InputSystem;

namespace HikersMod.Components;

public class SprintingController : MonoBehaviour
{
    private PlayerCharacterController _playerCharacter;

    public static SprintingController Instance { get; private set; }

    public bool IsSprinting { get; private set; }

    private void StartSprinting()
    {
        IsSprinting = true;
        _playerCharacter._runSpeed = Config.RunSpeed * Config.SprintMultiplier;
        _playerCharacter._strafeSpeed = Config.StrafeSpeed * Config.SprintMultiplier;
    }

    private void StopSprinting()
    {
        IsSprinting = false;
        _playerCharacter._runSpeed = Config.RunSpeed;
        _playerCharacter._strafeSpeed = Config.StrafeSpeed;
    }

    private void UpdateSprinting()
    {
        bool isOnValidGround = _playerCharacter.IsGrounded() && !_playerCharacter.IsSlidingOnIce();
        bool isTryingToMove = OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f;

        bool isSprintKeyboardKeyHeld = Keyboard.current != null && Keyboard.current[Config.SprintKey].isPressed;
        bool isSprintGamepadButtonHeld = Gamepad.current != null && Gamepad.current[Config.SprintGamepadButton].isPressed;
        bool isSprintButtonHeld = isSprintKeyboardKeyHeld || isSprintGamepadButtonHeld;

        bool wasSprintKeyboardKeyPressed = Keyboard.current != null && Keyboard.current[Config.SprintKey].wasPressedThisFrame;
        bool wasSprintGamepadButtonPressed = Gamepad.current != null && Gamepad.current[Config.SprintGamepadButton].wasPressedThisFrame;
        bool wasSprintButtonPressed = wasSprintKeyboardKeyPressed || wasSprintGamepadButtonPressed;

        bool isStartingVerticalThrust = OWInput.IsNewlyPressed(InputLibrary.thrustUp) || OWInput.IsNewlyPressed(InputLibrary.thrustDown);
        bool wasHoldingVerticalThrust = !isStartingVerticalThrust && (OWInput.IsPressed(InputLibrary.thrustUp) || OWInput.IsPressed(InputLibrary.thrustDown));

        bool canBoost = PlayerState.IsWearingSuit() && !Locator.GetPlayerSuit().IsTrainingSuit();
        bool isStartingBoost = canBoost && OWInput.IsPressed(InputLibrary.thrustUp) && OWInput.IsNewlyPressed(InputLibrary.boost);

        bool canStartSprint = isOnValidGround && isTryingToMove && wasSprintButtonPressed && !wasHoldingVerticalThrust;
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
        _playerCharacter = Locator.GetPlayerController();

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