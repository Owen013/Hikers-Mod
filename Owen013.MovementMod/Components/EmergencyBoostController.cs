using OWML.Common;
using UnityEngine;
using static HikersMod.ModMain;

namespace HikersMod.Components;

public class EmergencyBoostController : MonoBehaviour
{
    PlayerCharacterController _playerController;

    JetpackThrusterController _jetpackThruster;

    JetpackThrusterModel _jetpackThrusterModel;

    OWAudioSource _emergencyBoostAudio;

    ThrusterFlameController _downwardThrusterFlame;

    HUDHelmetAnimator _helmetAnimator;

    float _lastEmergencyBoostInputTime;

    float _lastEmergencyBoostTime;

    bool _isEmergencyBoosting;

    void ApplyEmergencyBoost()
    {
        _isEmergencyBoosting = true;
        _lastEmergencyBoostTime = Time.time;
        _jetpackThrusterModel._boostChargeFraction = 0f;
        _jetpackThruster._resources._currentFuel = Mathf.Max(0f, _jetpackThruster._resources.GetFuel() - Config.EmergencyBoostCost);
        float boostPower = Config.EmergencyBoostPower;
        
        // Change player velocity.
        Vector3 pointVelocity = _playerController._transform.InverseTransformDirection(_playerController._lastGroundBody.GetPointVelocity(_playerController._transform.position));
        Vector3 localVelocity = _playerController._transform.InverseTransformDirection(_playerController._owRigidbody.GetVelocity()) - pointVelocity;
        _playerController._owRigidbody.AddLocalVelocityChange(new Vector3(-localVelocity.x * 0.5f, boostPower - localVelocity.y * 0.5f, -localVelocity.z * 0.5f));

        // Play audio effect and show notification.
        _emergencyBoostAudio.pitch = Random.Range(1.0f, 1.4f);
        _emergencyBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, Config.EmergencyBoostVolume * 0.75f);
        _helmetAnimator.OnInstantDamage(boostPower, InstantDamageType.Impact);
        NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "EMERGENCY BOOST ACTIVATED", 3f), false);

        // Shake camera if enabled.
        if (Config.EmergencyBoostCameraShakeAmount > 0f)
            CameraShakerAPI?.ExplosionShake(strength: boostPower * Config.EmergencyBoostCameraShakeAmount);

        ModConsole?.WriteLine($"[{nameof(EmergencyBoostController)}] Super-Boosted", MessageType.Debug);
    }

    void EndEmergencyBoost()
    {
        _isEmergencyBoosting = false;
        _jetpackThrusterModel._chargeSeconds = _playerController.IsGrounded() ? _jetpackThrusterModel._chargeSecondsGround : _jetpackThrusterModel._chargeSecondsAir;
    }

    void OnConfigure()
    {
        enabled = Config.IsEmergencyBoostEnabled;
    }

    void Awake()
    {
        _playerController = GetComponent<PlayerCharacterController>();
        _jetpackThrusterModel = GetComponent<JetpackThrusterModel>();
        _jetpackThruster = GetComponent<JetpackThrusterController>();
        _helmetAnimator = GetComponentInChildren<HUDHelmetAnimator>();

        // Create audio source.
        _emergencyBoostAudio = new GameObject("HikersMod_EmergencyBoostAudioSrc").AddComponent<OWAudioSource>();
        _emergencyBoostAudio.transform.parent = GetComponentInChildren<PlayerAudioController>().transform;
        _emergencyBoostAudio.transform.localPosition = new Vector3(0, -1f, 1f);

        // Grab player model's downward thruster flame effect.
        var thrusters = _playerController.gameObject.GetComponentsInChildren<ThrusterFlameController>(true);
        foreach (ThrusterFlameController thruster in thrusters)
        {
            if (thruster._thruster == Thruster.Up_LeftThruster)
            {
                _downwardThrusterFlame = thruster;
                break;
            }
        }

        _playerController.OnBecomeGrounded += EndEmergencyBoost;

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    void LateUpdate()
    {
        bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
        bool canEmergencyBoost = _playerController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
        bool hasDoubleTappedEmergencyBoost = isInputting && Time.time - _lastEmergencyBoostInputTime < Config.EmergencyBoostInputTime;
        float lastWallJumpTime = Time.time - WallJumpController.Instance?.LastWallJumpTime ?? 0f;
        if (!canEmergencyBoost)
            EndEmergencyBoost();
        else if (hasDoubleTappedEmergencyBoost && lastWallJumpTime > 0.5f && _jetpackThruster._resources.GetFuel() > 0f && !_isEmergencyBoosting)
            ApplyEmergencyBoost();

        if (isInputting && canEmergencyBoost)
            _lastEmergencyBoostInputTime = Time.time;

        if (_isEmergencyBoosting)
            _jetpackThrusterModel._chargeSeconds = float.PositiveInfinity;

        float timeSinceBoost = Time.time - _lastEmergencyBoostTime;
        float thrusterCurve = -Mathf.Pow(5f * timeSinceBoost - 1f, 2f) + 1f;
        float thrusterScale = Mathf.Max(15f * thrusterCurve, _downwardThrusterFlame._currentScale);
        _downwardThrusterFlame.transform.localScale = Vector3.one * thrusterScale;
        _downwardThrusterFlame._light.range = _downwardThrusterFlame._baseLightRadius * thrusterScale;
        _downwardThrusterFlame._thrusterRenderer.enabled = thrusterScale > 0f;
        _downwardThrusterFlame._light.enabled = thrusterScale > 0f;
    }

    void OnDisable()
    {
        EndEmergencyBoost();
    }

    void OnDestroy()
    {
        Destroy(_emergencyBoostAudio);
        _playerController.OnBecomeGrounded -= EndEmergencyBoost;
        Config.OnConfigure -= OnConfigure;
    }
}