using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class CrouchController : MonoBehaviour
    {
        // makes the player crouch permanently
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAnimController), nameof(PlayerAnimController.LateUpdate))]
        public static void Animator_GetLayerWeight(PlayerAnimController __instance)
        {
            __instance._animator.SetLayerWeight(1, 1);
        }
    }
}