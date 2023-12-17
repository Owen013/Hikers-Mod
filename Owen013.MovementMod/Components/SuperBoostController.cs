using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class SuperBoostController : MonoBehaviour
{
    public static SuperBoostController s_instance;
    private float _lastBoostInputTime;
    private float _lastBoostTime;
    private bool _isSuperBoosting;
    private OWAudioSource _superBoostAudio;
    private JetpackThrusterModel _jetpackModel;
    private JetpackThrusterController _jetpackController;
    private PlayerCharacterController _characterController;
    private PlayerAudioController _audioController;
    private ThrusterFlameController _downThrustFlame;
    private HUDHelmetAnimator _helmetAnimator;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(SuperBoostController));
    }

    private void Update()
    {
        if (_characterController == null) return;
        bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
        bool canSuperBoost = _characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
        if (!canSuperBoost) EndSuperBoost();

        else if (ModController.s_instance.IsSuperBoostEnabled && isInputting && Time.time - _lastBoostInputTime < 0.25f  && _jetpackController._resources.GetFuel() > 0f && !_isSuperBoosting)
        {
            ApplySuperBoost();
        }

        if (isInputting && canSuperBoost) _lastBoostInputTime = Time.time;
        if (_isSuperBoosting) _jetpackModel._chargeSeconds = float.PositiveInfinity;
    }

    private void LateUpdate()
    {
        if (_characterController == null) return;
        float timeSinceBoost = Time.time - _lastBoostTime;
        float thrusterScale = Mathf.Clamp(Mathf.Max((-Mathf.Pow(5f * timeSinceBoost - 1f, 2f) + 1f) * ModController.s_instance.SuperBoostPower, 0f), _downThrustFlame._currentScale, 20f);
        _downThrustFlame.transform.localScale = Vector3.one * Mathf.Min(thrusterScale, 100f);
        _downThrustFlame._light.range = _downThrustFlame._baseLightRadius * thrusterScale;
        _downThrustFlame._thrusterRenderer.enabled = thrusterScale > 0f;
        _downThrustFlame._light.enabled = thrusterScale > 0f;
    }

    private void ApplySuperBoost()
    {
        _isSuperBoosting = true;
        _lastBoostTime = Time.time;
        _jetpackModel._boostChargeFraction = 0f;
        _jetpackController._resources._currentFuel = Mathf.Max(0f, _jetpackController._resources.GetFuel() - ModController.s_instance.SuperBoostCost);
        float boostPower = ModController.s_instance.SuperBoostPower;

        // April Fools
        if (Random.Range(0.0001f, 1.0000f) <= ModController.s_instance.SuperBoostMisfireChance)
        {
            NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "ERROR: EMERGENCY BOOST MISFIRE", 5f), false);
            TrippingController.s_instance.StartTripping();
            _characterController._owRigidbody.AddAngularVelocityChange(new(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
            _superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, Mathf.Min(boostPower * 0.05f, 20));
            _helmetAnimator.OnInstantDamage(boostPower, InstantDamageType.Impact);
            ModController.s_instance.CameraShakerAPI?.ExplosionShake(strength: boostPower);
            ModController.s_instance.DebugLog("Super boost misfired!");
            return;
        }

        // set player velocity
        Vector3 pointVelocity = _characterController._transform.InverseTransformDirection(_characterController._lastGroundBody.GetPointVelocity(_characterController._transform.position));
        Vector3 localVelocity = _characterController._transform.InverseTransformDirection(_characterController._owRigidbody.GetVelocity()) - pointVelocity;
        _characterController._owRigidbody.AddLocalVelocityChange(new Vector3(-localVelocity.x * 0.5f, boostPower - localVelocity.y * 0.75f, -localVelocity.z * 0.5f));

        // sound and visual effects
        _superBoostAudio.pitch = Random.Range(1.0f, 1.4f);
        _superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, Mathf.Min(boostPower * 0.05f, 20));
        _helmetAnimator.OnInstantDamage(boostPower, InstantDamageType.Impact);
        NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "EMERGENCY BOOST ACTIVATED", 5f), false);
        // if camerashaker is installed, do a camera shake
        ModController.s_instance.CameraShakerAPI?.ExplosionShake(strength: boostPower);

        ModController.s_instance.DebugLog("Super-Boosted");
    }

    private void EndSuperBoost()
    {
        _isSuperBoosting = false;
        _jetpackModel._chargeSeconds = _characterController.IsGrounded() ? _jetpackModel._chargeSecondsGround : _jetpackModel._chargeSecondsAir;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart()
    {
        s_instance._characterController = FindObjectOfType<PlayerCharacterController>();
        s_instance._audioController = FindObjectOfType<PlayerAudioController>();
        s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
        s_instance._jetpackController = FindObjectOfType<JetpackThrusterController>();
        s_instance._helmetAnimator = FindObjectOfType<HUDHelmetAnimator>();
        s_instance._superBoostAudio = new GameObject("HikersMod_SuperBoostAudioSrc").AddComponent<OWAudioSource>();
        s_instance._superBoostAudio.transform.parent = s_instance._audioController.transform;
        s_instance._superBoostAudio.transform.localPosition = new Vector3(0, 0, 1);
        var thrusters = Resources.FindObjectsOfTypeAll<ThrusterFlameController>();
        for (int i = 0; i < thrusters.Length; i++) if (thrusters[i]._thruster == Thruster.Up_LeftThruster) s_instance._downThrustFlame = thrusters[i];
        s_instance._characterController.OnBecomeGrounded += s_instance.EndSuperBoost;
    }
}