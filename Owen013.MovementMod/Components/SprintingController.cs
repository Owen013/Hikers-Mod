using UnityEngine;

namespace HikersMod.Components;

public class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprinting { get; private set; }

    private PlayerCharacterController _characterController;

    private IInputCommands _sprintButton;

    private void StartSprinting()
    {
        IsSprinting = true;
        _characterController._runSpeed = Config.RunSpeed * Config.SprintMultiplier;
        _characterController._strafeSpeed = Config.StrafeSpeed * Config.SprintMultiplier;
    }

    private void StopSprinting()
    {
        IsSprinting = false;
        _characterController._runSpeed = Config.RunSpeed;
        _characterController._strafeSpeed = Config.StrafeSpeed;
    }

    private void UpdateSprinting()
    {
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        if (enabled && isOnValidGround && OWInput.IsPressed(_sprintButton) && (IsSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
            StartSprinting();
        else
            StopSprinting();
    }

    private void OnConfigure()
    {
        enabled = Config.IsSprintingEnabled;

        // Change built-in character attributes
        _sprintButton = Config.SprintButton == "Up Thrust" ? InputLibrary.thrustUp : InputLibrary.thrustDown;

        UpdateSprinting();
    }

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();

        _characterController.OnBecomeGrounded += () =>
        {
            if (Config.ShouldSprintOnLanding)
                UpdateSprinting();
        };

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void Update()
    {
        // check both thrust buttons so that the player can thrust out of a sprint
        bool hasVerticalThrustChanged = OWInput.IsNewlyPressed(_sprintButton) || OWInput.IsNewlyReleased(_sprintButton) || OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown);
        if (hasVerticalThrustChanged || (OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded()))
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