using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class TrippingController : MonoBehaviour
{
    public static TrippingController s_instance;
    private PlayerCharacterController _characterController;
    private PlayerAudioController _audioController;
    private bool _isTripping;
    private float _tripTimeLeft;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(TrippingController));
    }

    private void FixedUpdate()
    {
        if (_characterController == null) return;

        if (_tripTimeLeft > 0f)
        {
            _tripTimeLeft -= Time.fixedDeltaTime;
        }
        else if (_isTripping && !PlayerState.IsDead())
        {
            StopTripping();
        }

        float trippingChance = SpeedController.s_instance.IsSprinting ? ModController.s_instance.SprintingTripChance : ModController.s_instance.TripChance;
        if (_tripTimeLeft <= 0 && _characterController.IsGrounded() && Random.Range(0.0001f, 1.0000f) <= 1f - Mathf.Pow(1f - trippingChance, Time.fixedDeltaTime))
        {
            StartTripping();
        }

    }

    public void StartTripping()
    {
        _isTripping = true;
        _tripTimeLeft = ModController.s_instance.TripDuration;
        _characterController.MakeUngrounded();
        _characterController._owRigidbody.UnfreezeRotation();
        _characterController.GetComponent<AlignPlayerWithForce>().enabled = false;
        _characterController.SetPhysicsMaterial(_characterController._standingPhysicMaterial);
        _audioController._oneShotSleepingAtCampfireSource.PlayOneShot(AudioType.PlayerGasp_Medium, 1f);
    }

    public void StopTripping()
    {
        _isTripping = false;
        _characterController._owRigidbody.FreezeRotation();
        _characterController.GetComponent<AlignPlayerWithForce>().enabled = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart()
    {
        s_instance._characterController = Locator.GetPlayerController();
        s_instance._audioController = Locator.GetPlayerAudioController();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.OnInstantDamage))]
    private static void OnCharacterControllerDamaged()
    {
        if (ModController.s_instance.TripWhenDamaged)
        {
            s_instance.StartTripping();
        }
    }
}
