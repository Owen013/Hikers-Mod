using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class WallJumpController : MonoBehaviour
{
    public static WallJumpController Instance { get; private set; }

    public float LastWallJumpTime { get; private set; }

    private PlayerCharacterController _characterController;

    private PlayerAnimController _animController;

    private PlayerImpactAudio _impactAudio;

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();
        _animController = GetComponentInChildren<PlayerAnimController>();
        _impactAudio = FindObjectOfType<PlayerImpactAudio>();
    }

    private void Update()
    {
        if (!Config.IsWallJumpingEnabled) return;

        _characterController.UpdatePushable();
        bool canWallJump = Time.time - LastWallJumpTime > 0.25f && _characterController._isPushable && !PlayerState.InZeroG() && !_characterController._isGrounded;
        if (canWallJump && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp))
        {
            Vector3 pointVelocity = _characterController._pushableBody.GetPointVelocity(_characterController._pushContactPt);
            Vector3 climbVelocity = new Vector3(0, 1f, -1f).normalized * Config.MaxJumpPower;

            if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20f)
            {
                ModMain.Instance.WriteLine($"[{nameof(WallJumpController)}] Can't Wall-Jump; going too fast", MessageType.Debug);
            }
            else
            {
                _characterController._owRigidbody.SetVelocity(pointVelocity);
                _characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
                _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                LastWallJumpTime = Time.time;
                ModMain.Instance.WriteLine($"[{nameof(WallJumpController)}] Wall-Jumped", MessageType.Debug);
            }
        }

        // Make player play fast freefall animation after each wall jump
        float freeFallSpeed = _animController._animator.GetFloat($"FreefallSpeed");
        float climbFraction = Mathf.Max(0, 1 - (Time.time - LastWallJumpTime));
        _animController._animator.SetFloat($"FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }
}