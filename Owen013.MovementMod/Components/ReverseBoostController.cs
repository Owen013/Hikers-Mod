using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class ReverseBoostController : MonoBehaviour
{
    public static ReverseBoostController s_instance;
    private bool _isBoostReversed;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(ReverseBoostController));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.ActivateBoost))]
    private static void OnStartBoosting(JetpackThrusterModel __instance)
    {
        if (ModController.s_instance.ReverseBoostChance != 0f && Random.Range(0f, 1f) <= ModController.s_instance.ReverseBoostChance)
        {
            s_instance._isBoostReversed = true;
            __instance._boostThrust = -__instance._boostThrust;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.DeactivateBoost))]
    private static void OnStopBoosting(JetpackThrusterModel __instance)
    {
        if (s_instance._isBoostReversed)
        {
            s_instance._isBoostReversed = false;
            __instance._boostThrust = -__instance._boostThrust;
        }
    }
}