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

    private float _wallJumpsLeft;

    private float _lastWallJumpRefill;

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();
        _animController = GetComponentInChildren<PlayerAnimController>();
        _impactAudio = FindObjectOfType<PlayerImpactAudio>();

        _characterController.OnBecomeGrounded += () =>
        {
            _wallJumpsLeft = ModMain.MaxWallJumps;
        };
    }

    private void Update()
    {
        if (!ModMain.Instance.IsWallJumpingEnabled) return;

        _characterController.UpdatePushable();
        bool canWallJump = _wallJumpsLeft > 0 && _characterController._isPushable && !PlayerState.InZeroG() && !_characterController._isGrounded;
        if (canWallJump && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp))
        {
            Vector3 pointVelocity = _characterController._pushableBody.GetPointVelocity(_characterController._pushContactPt);

            if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20f)
            {
                ModMain.Instance.WriteLine($"[{nameof(WallJumpController)}] Can't Wall-Jump; going too fast", MessageType.Debug);
            }
            else
            {
                _characterController._owRigidbody.SetVelocity(pointVelocity);
                _characterController._owRigidbody.AddLocalVelocityChange(Vector3.up * ModMain.Instance.MaxJumpPower * (_wallJumpsLeft / ModMain.MaxWallJumps));
                _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                _wallJumpsLeft--;
                LastWallJumpTime = Time.time;
                _lastWallJumpRefill = Time.time;
                ModMain.Instance.WriteLine($"[{nameof(WallJumpController)}] Wall-Jumped", MessageType.Debug);
            }
        }

        if (Time.time - _lastWallJumpRefill > 5 && _wallJumpsLeft < ModMain.MaxWallJumps)
        {
            _wallJumpsLeft++;
            _lastWallJumpRefill = Time.time;
        }

        // Make player play fast freefall animation after each wall jump
        float freeFallSpeed = _animController._animator.GetFloat($"FreefallSpeed");
        float climbFraction = 1 - (Time.time - LastWallJumpTime);
        _animController._animator.SetFloat($"FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }
}