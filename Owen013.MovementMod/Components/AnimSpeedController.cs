using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

[HarmonyPatch]
public class AnimSpeedController : MonoBehaviour
{
    private Animator _animator;

    private PlayerCharacterController _characterController;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _characterController = Locator.GetPlayerController();
    }

    private void LateUpdate()
    {
        float groundSpeed = _characterController.GetRelativeGroundVelocity().magnitude * (ModMain.Instance.SmolHatchlingAPI?.GetPlayerAnimSpeed() ?? 1);
        float animSpeedMultiplier = Mathf.Sqrt(groundSpeed / 6);
        float floatyPhysicsMultiplier = Mathf.Sqrt(_characterController._acceleration / ModMain.Instance.GroundAccel);
        float underwaterMultiplier = ModMain.Instance.ImmersionAPI != null ? ModMain.Instance.ImmersionAPI.GetAnimSpeed() : 1;

        _animator.speed = _characterController.IsGrounded() ? Mathf.Max(animSpeedMultiplier * floatyPhysicsMultiplier, floatyPhysicsMultiplier) : underwaterMultiplier;
    }

    // add component to animator(s)
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAnimController), nameof(PlayerAnimController.Start))]
    private static void AddToAnimController(PlayerAnimController __instance)
    {
        __instance.gameObject.AddComponent<AnimSpeedController>();
    }
}