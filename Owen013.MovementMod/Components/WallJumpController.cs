using OWML.Common;
using UnityEngine;
using static HikersMod.ModMain;

namespace HikersMod.Components;

class WallJumpController : MonoBehaviour
{
    public static WallJumpController Instance { get; private set; }

    public float LastWallJumpTime { get; private set; }

    PlayerCharacterController _characterController;

    PlayerAnimController _animController;

    PlayerImpactAudio _impactAudio;

    int _wallJumpsLeft;

    float _lastWallJumpRefill;

    void OnConfigure()
    {
        enabled = Config.IsWallJumpingEnabled;
    }

    void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();
        _animController = GetComponentInChildren<PlayerAnimController>();
        _impactAudio = FindObjectOfType<PlayerImpactAudio>();

        _characterController.OnBecomeGrounded += () =>
        {
            _wallJumpsLeft = Config.MaxWallJumps;
        };

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    void Update()
    {
        _characterController.UpdatePushable();
        bool canWallJump = _wallJumpsLeft > 0 && _characterController._isPushable && !PlayerState.InZeroG() && !_characterController._isGrounded;
        if (canWallJump && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp))
        {
            Vector3 pointVelocity = _characterController._pushableBody.GetPointVelocity(_characterController._pushContactPt);

            if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20f)
            {
                ModConsole?.WriteLine($"[{nameof(WallJumpController)}] Can't Wall-Jump; going too fast", MessageType.Debug);
            }
            else
            {
                _characterController._owRigidbody.SetVelocity(pointVelocity);
                _characterController._owRigidbody.AddLocalVelocityChange(Vector3.up * Config.MaxJumpPower * (_wallJumpsLeft / (float)Config.MaxWallJumps));
                _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                _wallJumpsLeft--;
                LastWallJumpTime = Time.time;
                _lastWallJumpRefill = Time.time;
                ModConsole?.WriteLine($"[{nameof(WallJumpController)}] Wall-Jumped", MessageType.Debug);
            }
        }

        if (Time.time - _lastWallJumpRefill > 5f && _wallJumpsLeft < Config.MaxWallJumps)
        {
            _wallJumpsLeft++;
            _lastWallJumpRefill = Time.time;
        }

        // Make player play fast freefall animation after each wall jump
        float freeFallSpeed = _animController._animator.GetFloat($"FreefallSpeed");
        float climbFraction = 1f - (Time.time - LastWallJumpTime);
        _animController._animator.SetFloat($"FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }

    void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}