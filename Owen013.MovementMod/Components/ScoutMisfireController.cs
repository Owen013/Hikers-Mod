using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class ScoutMisfireController : MonoBehaviour
{
    public static ScoutMisfireController Instance;
    private PlayerCharacterController _characterController;

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();
        _characterController.GetComponentInChildren<ProbeLauncher>().OnLaunchProbe += OnProbeLaunched;
    }

    private void OnProbeLaunched(SurveyorProbe probe)
    {
        if (Config.ScoutMisfireChance != 0f && Random.Range(0f, 1f) <= Config.ScoutMisfireChance)
        {
            TrippingController.Instance.StartTripping();
            Vector3 probeVelocity = probe._owRigidbody.GetRelativeVelocity(_characterController._owRigidbody);
            Instance._characterController._owRigidbody.AddVelocityChange(probeVelocity.normalized * 20f);
            probe._owRigidbody.AddVelocityChange(probeVelocity);
            Instance._characterController._owRigidbody.AddAngularVelocityChange(new(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)));
            NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "ERROR: SCOUT LAUNCHER MISFIRE", 5f), false);
        }
    }
}