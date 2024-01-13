using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class WallJumpController : MonoBehaviour
{
    public static WallJumpController s_instance;
    private PlayerCharacterController _characterController;
    private PlayerAnimController _animController;
    private PlayerImpactAudio _impactAudio;
    private float _wallJumpsLeft;
    private float _lastWallJumpTime;
    private float _lastWallJumpRefill;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(WallJumpController));
    }

    private void Update()
    {
        if (_characterController == null) return;

        UpdateWallJump();
    }

    private void UpdateWallJump()
    {
        _characterController.UpdatePushable();
        bool isWallJumpAllowed = (ModController.s_instance.wallJumpMode == "When Unsuited" && !PlayerState.IsWearingSuit()) || ModController.s_instance.wallJumpMode == "Always";
        if (isWallJumpAllowed && _characterController._isPushable && !PlayerState.InZeroG() && !_characterController._isGrounded && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && _wallJumpsLeft > 0)
        {
            OWRigidbody pushBody = _characterController._pushableBody;
            Vector3 pushPoint = _characterController._pushContactPt;
            Vector3 pointVelocity = pushBody.GetPointVelocity(pushPoint);
            Vector3 climbVelocity = new Vector3(0, ModController.s_instance.jumpPower * (_wallJumpsLeft / ModController.s_instance.wallJumpsPerJump), 0);

            if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20)
            {
                ModController.s_instance.DebugLog("Can't Wall-Jump; going too fast");
            }
            else
            {
                _characterController._owRigidbody.SetVelocity(pointVelocity);
                _characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
                _wallJumpsLeft -= 1;
                _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                _lastWallJumpTime = _lastWallJumpRefill = Time.time;
                ModController.s_instance.DebugLog("Wall-Jumped");
            }
        }

        // Replenish 1 wall jump if you hasn't done one for five seconds
        if (Time.time - _lastWallJumpRefill > 5 && _wallJumpsLeft < ModController.s_instance.wallJumpsPerJump)
        {
            _wallJumpsLeft += 1;
            _lastWallJumpRefill = Time.time;
        }

        // Make player play fast freefall animation after each wall jump
        float freeFallSpeed = _animController._animator.GetFloat("FreefallSpeed");
        float climbFraction = Mathf.Max(0, 1 - (Time.time - _lastWallJumpTime));
        _animController._animator.SetFloat("FreefallSpeed", Mathf.Max(freeFallSpeed, climbFraction));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void CharacterControllerStart()
    {
        s_instance._characterController = FindObjectOfType<PlayerCharacterController>();
        s_instance._animController = FindObjectOfType<PlayerAnimController>();
        s_instance._impactAudio = FindObjectOfType<PlayerImpactAudio>();
        s_instance._characterController.OnBecomeGrounded += () => s_instance._wallJumpsLeft = ModController.s_instance.wallJumpsPerJump;
    }
}