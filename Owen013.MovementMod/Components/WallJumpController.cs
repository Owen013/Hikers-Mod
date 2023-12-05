using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class WallJumpController : MonoBehaviour
    {
        public static WallJumpController Instance;
        public PlayerCharacterController _characterController;
        public PlayerAnimController _animController;
        public PlayerImpactAudio _impactAudio;
        public float _wallJumpsLeft;
        public float _lastWallJumpTime;
        public float _lastWallJumpRefill;

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(WallJumpController));
        }

        public void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (!_characterController) return;

            // Update everthing else
            UpdateWallJump();
        }

        public void UpdateWallJump()
        {
            _characterController.UpdatePushable();
            if (((HikersMod.Instance._wallJumpEnabledMode == "When Unsuited" && !PlayerState.IsWearingSuit()) || HikersMod.Instance._wallJumpEnabledMode == "Always") &&
                _characterController._isPushable &&
                !PlayerState.InZeroG() &&
                !_characterController._isGrounded &&
                OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) &&
                _wallJumpsLeft > 0)
            {
                OWRigidbody pushBody = _characterController._pushableBody;
                Vector3 pushPoint = _characterController._pushContactPt;
                Vector3 pointVelocity = pushBody.GetPointVelocity(pushPoint);
                Vector3 climbVelocity = new Vector3(0, HikersMod.Instance._jumpPower * (_wallJumpsLeft / HikersMod.Instance._wallJumpsPerJump), 0);

                if ((pointVelocity - _characterController._owRigidbody.GetVelocity()).magnitude > 20) HikersMod.Instance.DebugLog("Can't Wall-Jump; going too fast");
                else
                {
                    _characterController._owRigidbody.SetVelocity(pointVelocity);
                    _characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
                    _wallJumpsLeft -= 1;
                    _impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                    _lastWallJumpTime = _lastWallJumpRefill = Time.time;
                    HikersMod.Instance.DebugLog("Wall-Jumped");
                }
            }

            // Replenish 1 wall jump if the player hasn't done one for five seconds
            if (Time.time - _lastWallJumpRefill > 5 && _wallJumpsLeft < HikersMod.Instance._wallJumpsPerJump)
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
        public static void CharacterControllerStart()
        {
            Instance._characterController = FindObjectOfType<PlayerCharacterController>();
            Instance._animController = FindObjectOfType<PlayerAnimController>();
            Instance._impactAudio = FindObjectOfType<PlayerImpactAudio>();

            Instance._characterController.OnBecomeGrounded += () =>
            {
                Instance._wallJumpsLeft = HikersMod.Instance._wallJumpsPerJump;
            };
        }
    }
}