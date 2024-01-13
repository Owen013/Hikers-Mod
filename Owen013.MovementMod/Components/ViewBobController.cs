using HarmonyLib;
using System.Dynamic;
using System.Net.Security;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HikersMod.Components
{
    public class ViewBobController : MonoBehaviour
    {
        public static ViewBobController s_instance;
        private PlayerCharacterController _characterController;
        private PlayerCameraController _cameraController;
        private PlayerAnimController _animController;
        private GameObject _bobRoot;
        private float _lastGroundedTime;
        private float _bobTime;
        private float _bobIntensity;

        public void Awake()
        {
            s_instance = this;
            Harmony.CreateAndPatchAll(typeof(ViewBobController));
        }

        public void FixedUpdate()
        {
            if (!_characterController) return;

            AnimatorStateInfo animInfo = _animController._animator.GetCurrentAnimatorStateInfo(0);
            if (Time.fixedTime - _lastGroundedTime < 1f)
            {
                _bobIntensity = Mathf.Lerp(_bobIntensity, 0f, 0.5f);
            }
            else
            {
                _bobTime = animInfo.normalizedTime + 0.5f; // thank you Etherpod!
                _bobIntensity = Mathf.Lerp(_bobIntensity, Mathf.Sqrt(Mathf.Pow(_animController._animator.GetFloat("RunSpeedX"), 2f) + Mathf.Pow(_animController._animator.GetFloat("RunSpeedY"), 2f)) * 0.02f, 0.5f);
            }
            float bobX = Mathf.Sin(2f * Mathf.PI * _bobTime);
            float bobY = Mathf.Cos(4f * Mathf.PI * _bobTime);

            _bobRoot.transform.localPosition = new Vector3(bobX * _bobIntensity * ModController.s_instance.viewBobXSensitivity, bobY * _bobIntensity * ModController.s_instance.viewBobYSensitivity, 0f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void OnCharacterControllerStart()
        {
            s_instance._characterController = Locator.GetPlayerController();
            s_instance._cameraController = FindObjectOfType<PlayerCameraController>();
            s_instance._animController = FindObjectOfType<PlayerAnimController>();

            // create viewbob root and parent camera to it
            s_instance._bobRoot = new();
            s_instance._bobRoot.name = "ViewBobRoot";
            s_instance._bobRoot.transform.parent = s_instance._cameraController._playerCamera.mainCamera.transform.parent;
            s_instance._bobRoot.transform.localPosition = Vector3.zero;
            s_instance._bobRoot.transform.localRotation = Quaternion.identity;
            s_instance._cameraController._playerCamera.mainCamera.transform.parent = s_instance._bobRoot.transform;

            s_instance._characterController.OnBecomeGrounded += () =>
            {
                s_instance._lastGroundedTime = Time.fixedTime;
            };
        }
    }
}