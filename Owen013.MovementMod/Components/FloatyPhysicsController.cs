using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public  class FloatyPhysicsController : MonoBehaviour
    {
        public static FloatyPhysicsController Instance;
        public PlayerCharacterController _characterController;

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(FloatyPhysicsController));
        }

        public void Update()
        {
            if (HikersMod.Instance._isFloatyPhysicsEnabled) UpdateAcceleration();
        }

        public void UpdateAcceleration()
        {
            if (!_characterController) return;
            float gravMultiplier;
            if (_characterController.IsGrounded() && !_characterController.IsSlidingOnIce()) gravMultiplier = Mathf.Min(Mathf.Pow(_characterController.GetNormalAccelerationScalar() / 12, HikersMod.Instance._floatyPhysicsPower), 1);
            else gravMultiplier = 1;
            _characterController._acceleration = HikersMod.Instance._groundAccel * gravMultiplier;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void OnCharacterControllerStart()
        {
            Instance._characterController = Locator.GetPlayerController();
        }
    }
}