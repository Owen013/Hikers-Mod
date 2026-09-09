using UnityEngine;

namespace HikersMod.Components;

class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprinting { get; private set; }

    PlayerCharacterController _playerController;

    IInputCommands _sprintButton;

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
        if (enabled && isOnValidGround && OWInput.IsPressed(_sprintButton) && (IsSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
            StartSprinting();
        else
            StopSprinting();
    }

    void OnConfigure()
    {
        enabled = Config.IsSprintingEnabled;

        _sprintButton = Config.SprintButton switch
        {
            "Down Thrust" => InputLibrary.thrustDown,
            _ => InputLibrary.thrustUp
        };

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
        // check both thrust buttons so that the player can thrust out of a sprint
        if (OWInput.IsNewlyPressed(_sprintButton) || OWInput.IsNewlyReleased(_sprintButton) || (OWInput.IsNewlyPressed(InputLibrary.boost) && !_playerController.IsGrounded()))
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