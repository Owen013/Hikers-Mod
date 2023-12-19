using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class ReverseRepairController : MonoBehaviour
{
    public static ReverseRepairController s_instance;
    private bool _isRepairReversed;

    private void Awake()
    {
        s_instance = this;
        GlobalMessenger.AddListener("StartRepairing", OnStartRepairing);
        Harmony.CreateAndPatchAll(typeof(ReverseRepairController));
    }

    private void OnStartRepairing()
    {
        if (ModController.s_instance.ReverseRepairChance != 0f && Random.Range(0f, 1f) <= ModController.s_instance.ReverseRepairChance)
        {
            _isRepairReversed = true;
        }
        else
        {
            _isRepairReversed = false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.RepairTick))]
    public static bool RepairTick(ShipComponent __instance)
    {
        if (!__instance._damaged) return false;

        float repairAmount = Time.deltaTime / __instance._repairTime;
        if (s_instance._isRepairReversed) repairAmount = -repairAmount;
        __instance._repairFraction = Mathf.Clamp01(__instance._repairFraction + repairAmount);

        if (__instance._repairFraction >= 1f)
        {
            __instance.SetDamaged(false);
        }
        else if (__instance._repairFraction <= 0f)
        {
            __instance.TriggerSystemFailure();
        }

        if (__instance._damageEffect)
        {
            __instance._damageEffect.SetEffectBlend(1f - __instance._repairFraction);
        }

        return false;
    }
}