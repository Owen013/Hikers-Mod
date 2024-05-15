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
            _wallJumpsLeft = Config.MaxWallJumps;
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
            Vector3 pointVelocity = _characterController._pushableBody.GetPointVelocity(_characterController._pushContactPt);
            Vector3 climbVelocity = new Vector3(0, Config.MaxJumpPower, 0f) * (_wallJumpsLeft / Config.MaxWallJumps);

            if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20f)
            {
                ModMain.Instance.WriteLine($"[{nameof(WallJumpController)}] Can't Wall-Jump; going too fast", MessageType.Debug);
            }
            else
            {
                _characterController._owRigidbody.SetVelocity(pointVelocity);
                _characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
                _wallJumpsLeft -= 1;
                _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                LastWallJumpTime = _lastWallJumpRefill = Time.time;
                ModMain.Instance.WriteLine($"[{nameof(WallJumpController)}] Wall-Jumped", MessageType.Debug);
            }
        }

        // Replenish 1 wall jump if you hasn't done one for five seconds
        if (Time.time - _lastWallJumpRefill > 5 && _wallJumpsLeft < Config.MaxWallJumps)
        {
            _wallJumpsLeft += 1;
            _lastWallJumpRefill = Time.time;
        }

        // Make player play fast freefall animation after each wall jump
        float freeFallSpeed = _animController._animator.GetFloat($"FreefallSpeed");
        float climbFraction = Mathf.Max(0, 1 - (Time.time - LastWallJumpTime));
        _animController._animator.SetFloat($"FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }
}