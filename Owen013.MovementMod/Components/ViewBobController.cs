using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class ViewBobController : MonoBehaviour
    {
        public static ViewBobController Instance;
        public PlayerCharacterController _characterController;
        public PlayerAnimController _animController;
        public PlayerCameraController _cameraController;
        public float bobTime;

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(ViewBobController));
        }

        public void Update()
        {
            if (!_animController) return;
            bobTime += HikersMod.Instance._animSpeed * (_characterController.GetRelativeGroundVelocity().magnitude / 6) / 2 * Time.deltaTime;
            if (bobTime > 1) bobTime -= 1;
            float bobX = Mathf.Sin(Mathf.PI * bobTime);
            float bobY = (Mathf.Cos(Mathf.PI * bobTime) + 1f) / 2f;
            _cameraController._origLocalPosition = new Vector3(bobX, 0.8496f + bobY, 0.15f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void OnAnimControllerStart(PlayerCharacterController __instance)
        {
            Instance._characterController = __instance;
            Instance._animController = __instance.gameObject.GetComponentInChildren<PlayerAnimController>();
            Instance._cameraController = Locator.GetPlayerCameraController();
        }
    }
}