using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class WallJumpController : MonoBehaviour
{
    private PlayerCharacterController _characterController;
    private PlayerAnimController _animController;
    private PlayerImpactAudio _impactAudio;
    private float _wallJumpsLeft;
    private float _lastWallJumpTime;
    private float _lastWallJumpRefill;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _animController = GetComponentInChildren<PlayerAnimController>();
        _impactAudio = FindObjectOfType<PlayerImpactAudio>();

        _characterController.OnBecomeGrounded += () =>
        {
            _wallJumpsLeft = Config.WallJumpsPerJump;
        };
    }

    private void Update()
    {
        UpdateWallJump();
    }

    private void UpdateWallJump()
    {
        _characterController.UpdatePushable();
        bool isWallJumpAllowed = (Config.WallJumpMode == "When Unsuited" && !PlayerState.IsWearingSuit()) || Config.WallJumpMode == "Always";
        bool canWallJump = isWallJumpAllowed && _characterController._isPushable && !PlayerState.InZeroG() && !_characterController._isGrounded && _wallJumpsLeft > 0;
        if (isWallJumpAllowed && canWallJump && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character))
        {
            OWRigidbody pushBody = _characterController._pushableBody;
            Vector3 pushPoint = _characterController._pushContactPt;
            Vector3 pointVelocity = pushBody.GetPointVelocity(pushPoint);
            Vector3 climbVelocity = new Vector3(0, Config.MaxJumpPower, 0f) * (_wallJumpsLeft / Config.WallJumpsPerJump);

            if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20f)
            {
                Main.WriteLine($"[{nameof(WallJumpController)}] Can't Wall-Jump; going too fast", MessageType.Debug);
            }
            else
            {
                _characterController._owRigidbody.SetVelocity(pointVelocity);
                _characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
                _wallJumpsLeft -= 1;
                _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                _lastWallJumpTime = _lastWallJumpRefill = Time.time;
                Main.WriteLine($"[{nameof(WallJumpController)}] Wall-Jumped", MessageType.Debug);
            }
        }

        // Replenish 1 wall jump if you hasn't done one for five seconds
        if (Time.time - _lastWallJumpRefill > 5 && _wallJumpsLeft < Config.WallJumpsPerJump)
        {
            _wallJumpsLeft += 1;
            _lastWallJumpRefill = Time.time;
        }

        // Make player play fast freefall animation after each wall jump
        float freeFallSpeed = _animController._animator.GetFloat($"FreefallSpeed");
        float climbFraction = Mathf.Max(0, 1 - (Time.time - _lastWallJumpTime));
        _animController._animator.SetFloat($"FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }
}