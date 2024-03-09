using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class ReverseBoostController : MonoBehaviour
{
    public static ReverseBoostController Instance;
    private bool _isBoostReversed;

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(typeof(ReverseBoostController));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.ActivateBoost))]
    private static void OnStartBoosting(JetpackThrusterModel __instance)
    {
        if (Config.ReverseBoostChance != 0f && Random.Range(0f, 1f) <= Config.ReverseBoostChance)
        {
            Instance._isBoostReversed = true;
            __instance._boostThrust = -__instance._boostThrust;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.DeactivateBoost))]
    private static void OnStopBoosting(JetpackThrusterModel __instance)
    {
        if (Instance._isBoostReversed)
        {
            Instance._isBoostReversed = false;
            __instance._boostThrust = -__instance._boostThrust;
        }
    }
}