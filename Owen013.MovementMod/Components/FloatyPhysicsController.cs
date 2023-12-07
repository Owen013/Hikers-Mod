using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class FloatyPhysicsController : MonoBehaviour
    {
        public static FloatyPhysicsController s_instance;
        public PlayerCharacterController _characterController;

        public void Awake()
        {
            s_instance = this;
            Harmony.CreateAndPatchAll(typeof(FloatyPhysicsController));
        }

        public void Update()
        {
            if (HikersMod.s_instance._isFloatyPhysicsEnabled) UpdateAcceleration();
        }

        public void UpdateAcceleration()
        {
            if (!_characterController) return;
            float gravMultiplier = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce() ? Mathf.Min(Mathf.Pow(_characterController.GetNormalAccelerationScalar() / 12, HikersMod.s_instance._floatyPhysicsPower), 1) : 1;
            _characterController._acceleration = HikersMod.s_instance._groundAccel * gravMultiplier;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void OnCharacterControllerStart()
        {
            s_instance._characterController = Locator.GetPlayerController();
        }
    }
}