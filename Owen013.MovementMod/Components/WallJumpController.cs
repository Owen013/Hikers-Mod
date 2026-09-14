using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class WallJumpController : MonoBehaviour
{
    private PlayerCharacterController _playerCharacter;

    private PlayerAnimController _playerAnimController;

    private PlayerImpactAudio _playerImpactAudio;

    private int _wallJumpsLeft;

    private float _lastWallJumpRefill;

    public static WallJumpController Instance { get; private set; }

    public float LastWallJumpTime { get; private set; }

    private void OnConfigure()
    {
        enabled = Config.IsWallJumpingEnabled;
    }

    private void Awake()
    {
        Instance = this;
        _playerCharacter = GetComponent<PlayerCharacterController>();
        _playerAnimController = GetComponentInChildren<PlayerAnimController>();
        _playerImpactAudio = FindObjectOfType<PlayerImpactAudio>();

        // Reset wall jump budget when player lands.
        _playerCharacter.OnBecomeGrounded += () =>
        {
            _wallJumpsLeft = Config.MaxWallJumps;
        };

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void Update()
    {
        _playerCharacter.UpdatePushable();
        bool canWallJump = _wallJumpsLeft > 0 && _playerCharacter._isPushable && !PlayerState.InZeroG() && !_playerCharacter._isGrounded;
        if (canWallJump && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp))
        {
            Vector3 pointVelocity = _playerCharacter._pushableBody.GetPointVelocity(_playerCharacter._pushContactPt);
            float relativeSpeed = (pointVelocity - _playerCharacter._owRigidbody.GetVelocity()).magnitude;
            if (relativeSpeed <= 20f)
            {
                _playerCharacter._owRigidbody.SetVelocity(pointVelocity);
                _playerCharacter._owRigidbody.AddLocalVelocityChange(Vector3.up * Config.MaxJumpPower * (_wallJumpsLeft / (float)Config.MaxWallJumps));
                _playerImpactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                _wallJumpsLeft--;
                LastWallJumpTime = Time.time;
                _lastWallJumpRefill = Time.time;
            }
        }

        if (Time.time - _lastWallJumpRefill > 5f && _wallJumpsLeft < Config.MaxWallJumps)
        {
            _wallJumpsLeft++;
            _lastWallJumpRefill = Time.time;
        }

        // Make player play fast freefall animation after each wall jump.
        float freeFallSpeed = _playerAnimController._animator.GetFloat($"FreefallSpeed");
        float climbFraction = 1f - (Time.time - LastWallJumpTime);
        _playerAnimController._animator.SetFloat($"FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}