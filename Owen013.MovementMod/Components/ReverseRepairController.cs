using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class ReverseRepairController : MonoBehaviour
{
    public static ReverseRepairController Instance;
    private bool _isRepairReversed;

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(typeof(ReverseRepairController));
    }

    private void Update()
    {
        if (OWInput.IsNewlyPressed(InputLibrary.interact))
        {
            if (Config.ReverseRepairChance != 0f && Random.Range(0f, 1f) <= Config.ReverseRepairChance)
            {
                Main.WriteLine("Repair reversed!");
                _isRepairReversed = true;
            }
            else
            {
                _isRepairReversed = false;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.RepairTick))]
    public static bool RepairTick(ShipHull __instance)
    {
        if (!Instance._isRepairReversed)
        {
            return true;
        }

        if (!__instance._damaged)
        {
            return false;
        }

        __instance._integrity = Mathf.Min(__instance._integrity - Time.deltaTime / __instance._repairTime, 1f);
        if (__instance._integrity <= 0f)
        {
            __instance.GetComponent<ShipDetachableModule>().Detach();
        }
        if (__instance._damageEffect != null)
        {
            __instance._damageEffect.SetEffectBlend(1f - __instance._integrity);
        }

        return false;
    }
}