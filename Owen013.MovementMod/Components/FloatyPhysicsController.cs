using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class FloatyPhysicsController : MonoBehaviour
{
    public static FloatyPhysicsController s_instance;
    private PlayerCharacterController _characterController;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(FloatyPhysicsController));
    }

    private void Update()
    {
        if (ModController.s_instance.IsFloatyPhysicsEnabled) UpdateAcceleration();
    }

    private void UpdateAcceleration()
    {
        if (_characterController == null) return;
        float gravMultiplier = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce() ? Mathf.Clamp(Mathf.Pow(_characterController.GetNormalAccelerationScalar() / 12, ModController.s_instance.FloatyPhysicsPower), 0.25f, 1f) : 1f;
        _characterController._acceleration = ModController.s_instance.GroundAccel * gravMultiplier;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart()
    {
        s_instance._characterController = Locator.GetPlayerController();
    }
}