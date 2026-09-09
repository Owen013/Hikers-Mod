using OWML.Common;
using UnityEngine;
using static HikersMod.ModMain;

namespace HikersMod.Components;

class WallJumpController : MonoBehaviour
{
    public static WallJumpController Instance { get; private set; }

    public float LastWallJumpTime { get; private set; }

    PlayerCharacterController _playerController;

    PlayerAnimController _playerAnimController;

    PlayerImpactAudio _playerImpactAudio;

    int _wallJumpsLeft;

    float _lastWallJumpRefill;

    void OnConfigure()
    {
        enabled = Config.IsWallJumpingEnabled;
    }

    void Awake()
    {
        Instance = this;
        _playerController = GetComponent<PlayerCharacterController>();
        _playerAnimController = GetComponentInChildren<PlayerAnimController>();
        _playerImpactAudio = FindObjectOfType<PlayerImpactAudio>();

        // Reset wall jump budget when player lands.
        _playerController.OnBecomeGrounded += () =>
        {
            _wallJumpsLeft = Config.MaxWallJumps;
        };

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    void Update()
    {
        _playerController.UpdatePushable();
        bool canWallJump = _wallJumpsLeft > 0 && _playerController._isPushable && !PlayerState.InZeroG() && !_playerController._isGrounded;
        if (canWallJump && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp))
        {
            Vector3 pointVelocity = _playerController._pushableBody.GetPointVelocity(_playerController._pushContactPt);

            if ((pointVelocity - _playerController._owRigidbody.GetVelocity()).magnitude > 20f)
            {
                ModConsole?.WriteLine($"[{nameof(WallJumpController)}] Can't Wall-Jump; going too fast", MessageType.Debug);
            }
            else
            {
                _playerController._owRigidbody.SetVelocity(pointVelocity);
                _playerController._owRigidbody.AddLocalVelocityChange(Vector3.up * Config.MaxJumpPower * (_wallJumpsLeft / (float)Config.MaxWallJumps));
                _playerImpactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
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
        float freeFallSpeed = _playerAnimController._animator.GetFloat($"FreefallSpeed");
        float climbFraction = 1f - (Time.time - LastWallJumpTime);
        _playerAnimController._animator.SetFloat($"FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }

    void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}