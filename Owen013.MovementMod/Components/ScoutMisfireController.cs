using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class ScoutMisfireController : MonoBehaviour
{
    public static ScoutMisfireController s_instance;
    private PlayerCharacterController _characterController;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(ScoutMisfireController));
    }

    private void OnProbeLaunched(SurveyorProbe probe)
    {
        if (ModController.s_instance.ScoutMisfireChance != 0f && Random.Range(0f, 1f) <= ModController.s_instance.ScoutMisfireChance)
        {
            TrippingController.s_instance.StartTripping();
            Vector3 probeVelocity = probe._owRigidbody.GetRelativeVelocity(_characterController._owRigidbody);
            s_instance._characterController._owRigidbody.AddVelocityChange(probeVelocity.normalized * 20f);
            probe._owRigidbody.AddVelocityChange(probeVelocity);
            s_instance._characterController._owRigidbody.AddAngularVelocityChange(new(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)));
            NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "ERROR: SCOUT LAUNCHER MISFIRE", 5f), false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart(PlayerCharacterController __instance)
    {
        s_instance._characterController = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.Start))]
    private static void OnProbeLauncherStart(ProbeLauncher __instance)
    {
        if (__instance.GetComponentInParent<PlayerCharacterController>())
        {
            __instance.OnLaunchProbe += s_instance.OnProbeLaunched;
        }
    }
}