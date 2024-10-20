using UnityEngine;

namespace HikersMod.Components;

public class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprinting { get; private set; }

    private PlayerCharacterController _characterController;

    private IInputCommands _sprintButton;

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();

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

        if (IsSprinting)
        {
            _characterController._runSpeed = ModMain.RunSpeed * ModMain.SprintMultiplier;
            _characterController._strafeSpeed = ModMain.StrafeSpeed * ModMain.SprintMultiplier;
        }
        else
        {
            _characterController._runSpeed = ModMain.RunSpeed;
            _characterController._strafeSpeed = ModMain.StrafeSpeed;
        }
    }

    private void OnDisable()
    {
        IsSprinting = false;
    }

    private void UpdateSprinting()
    {
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();

        if (ModMain.IsSprintingEnabled && isOnValidGround && OWInput.IsPressed(_sprintButton) && (IsSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
        {
            IsSprinting = true;
        }
        else
        {
            IsSprinting = false;
        }
    }
}