using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class TrippingController : MonoBehaviour
{
    public static TrippingController Instance;
    private PlayerCharacterController _characterController;
    private PlayerAudioController _audioController;
    private bool _isTripping;
    private float _tripTimeLeft;

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();
        _audioController = Locator.GetPlayerAudioController();
        Harmony.CreateAndPatchAll(typeof(TrippingController));
    }

    private void FixedUpdate()
    {
        if (_tripTimeLeft > 0f)
        {
            _tripTimeLeft -= Time.deltaTime;
        }
        else if (_isTripping && !PlayerState.IsDead())
        {
            StopTripping();
        }

        float trippingChance = SpeedController.Instance.IsSprinting ? Config.SprintingTripChance : Config.TripChance;
        bool canTrip = _tripTimeLeft <= 0 && _characterController.IsGrounded();
        if (trippingChance != 0f && canTrip && Random.Range(0f, 1f) <= 1f - Mathf.Pow(1f - trippingChance, Time.deltaTime))
        {
            StartTripping();
        }

    }

    public void StartTripping()
    {
        _isTripping = true;
        _tripTimeLeft = Config.TripDuration;
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
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.OnInstantDamage))]
    private static void OnCharacterControllerDamaged(float instantDamage)
    {
        if (Random.Range(0f, 1f) <= Config.DamagedTripChance * instantDamage)
        {
            Instance.StartTripping();
        }
    }
}
