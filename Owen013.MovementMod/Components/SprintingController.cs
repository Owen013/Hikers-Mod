using UnityEngine;
using UnityEngine.InputSystem;

namespace HikersMod.Components;

public class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprinting { get; private set; }

    PlayerCharacterController _playerController;

    void StartSprinting()
    {
        IsSprinting = true;
        _playerController._runSpeed = Config.RunSpeed * Config.SprintMultiplier;
        _playerController._strafeSpeed = Config.StrafeSpeed * Config.SprintMultiplier;
    }

    void StopSprinting()
    {
        IsSprinting = false;
        _playerController._runSpeed = Config.RunSpeed;
        _playerController._strafeSpeed = Config.StrafeSpeed;
    }

    void UpdateSprinting()
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

    void OnConfigure()
    {
        enabled = Config.IsSprintingEnabled;

        UpdateSprinting();
    }

    void Awake()
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

    void Update()
    {
        //bool wasSprintButtonPressed = false;
        //bool wasSprintButtonReleased = false;
        //if (Keyboard.current != null)
        //{
        //    wasSprintButtonPressed = Keyboard.current[Config.SprintKey].wasPressedThisFrame;
        //    wasSprintButtonReleased = Keyboard.current[Config.SprintKey].wasReleasedThisFrame;
        //}
        //if (Gamepad.current != null)
        //{
        //    wasSprintButtonPressed = wasSprintButtonPressed || Gamepad.current[Config.SprintGamepadButton].wasPressedThisFrame;
        //    wasSprintButtonReleased = wasSprintButtonReleased || Gamepad.current[Config.SprintGamepadButton].wasReleasedThisFrame;
        //}

        //bool isBoostingInAir = OWInput.IsNewlyPressed(InputLibrary.boost) && !_playerController.IsGrounded();
        //if (wasSprintButtonPressed || wasSprintButtonReleased || isBoostingInAir)
        //{
        //    UpdateSprinting();
        //}

        UpdateSprinting();
    }

    void OnDisable()
    {
        StopSprinting();
    }

    void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}