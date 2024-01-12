using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class ViewBobController : MonoBehaviour
    {
        public static ViewBobController s_instance;
        private PlayerCharacterController _characterController;
        private PlayerCameraController _cameraController;
        private PlayerAnimController _animController;
        private float _lastFootLiftTime;
        private string _lastFootLifted;
        private float _timePosition;
        private float _intensity;

        public void Awake()
        {
            s_instance = this;
            Harmony.CreateAndPatchAll(typeof(ViewBobController));
        }

        public void FixedUpdate()
        {
            if (!_characterController) return;

            _timePosition += Time.fixedDeltaTime * _animController._animator.speed;
            _intensity = Mathf.Lerp(_intensity, _characterController.GetRelativeGroundVelocity().magnitude / 6, 0.25f);

            float bobY = (Mathf.Cos(Mathf.PI * _timePosition) + 1) * 0.5f;

            _cameraController._origLocalPosition = new Vector3(0f, 0.8496f + bobY * 0.5f, 0.15f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void OnCharacterControllerStart(PlayerCharacterController __instance)
        {
            s_instance._characterController = __instance;
            s_instance._cameraController = FindObjectOfType<PlayerCameraController>();

            s_instance._animController = FindObjectOfType<PlayerAnimController>();
            s_instance._animController.OnRightFootLift += () =>
            {
                s_instance._lastFootLiftTime = Time.fixedTime;
                s_instance._lastFootLifted = "right";
            };
            s_instance._animController.OnLeftFootLift += () =>
            {
                s_instance._lastFootLiftTime = Time.fixedTime;
                s_instance._lastFootLifted = "left";
            };

            s_instance._timePosition = 0f;
        }
    }
}