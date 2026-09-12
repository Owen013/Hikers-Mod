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
        bool sprintKeyPressed = Keyboard.current != null && Keyboard.current[Config.SprintKey].isPressed;
        bool sprintButtonPressed = Gamepad.current != null && Gamepad.current[Config.SprintGamepadButton].isPressed;
        if (enabled && isOnValidGround && (sprintKeyPressed || sprintButtonPressed) && (IsSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
            StartSprinting();
        else
            StopSprinting();
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
                UpdateSprinting();
        };

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    void Update()
    {
        bool sprintKeyChanged = Keyboard.current != null && (Keyboard.current[Config.SprintKey].wasPressedThisFrame || Keyboard.current[Config.SprintKey].wasReleasedThisFrame);
        bool sprintButtonChanged = Gamepad.current != null && (Gamepad.current[Config.SprintGamepadButton].wasPressedThisFrame || Gamepad.current[Config.SprintGamepadButton].wasReleasedThisFrame);
        if (sprintKeyChanged || sprintButtonChanged || (OWInput.IsNewlyPressed(InputLibrary.boost) && !_playerController.IsGrounded()))
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