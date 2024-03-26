using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class EmergencyBoostController : MonoBehaviour
{
    private OWAudioSource _superBoostAudio;
    private JetpackThrusterModel _jetpackModel;
    private JetpackThrusterController _jetpackController;
    private PlayerCharacterController _characterController;
    private PlayerAudioController _audioController;
    private ThrusterFlameController _downThrustFlame;
    private HUDHelmetAnimator _helmetAnimator;
    private float _lastBoostInputTime;
    private float _lastBoostTime;
    private bool _isEmergencyBoosting;

    private void Awake()
    {
        _characterController = Locator.GetPlayerController();
        _audioController = Locator.GetPlayerAudioController();
        _jetpackModel = FindObjectOfType<JetpackThrusterModel>();
        _jetpackController = FindObjectOfType<JetpackThrusterController>();
        _helmetAnimator = FindObjectOfType<HUDHelmetAnimator>();

        // create super boost audio source
        _superBoostAudio = new GameObject("HikersMod_EmergencyBoostAudioSrc").AddComponent<OWAudioSource>();
        _superBoostAudio.transform.parent = _audioController.transform;
        _superBoostAudio.transform.localPosition = new Vector3(0, -1f, 1f);

        // get player's downward thruster flame
        var thrusters = _characterController.gameObject.GetComponentsInChildren<ThrusterFlameController>(includeInactive: true);
        for (int i = 0; i < thrusters.Length; i++)
        {
            if (thrusters[i]._thruster == Thruster.Up_LeftThruster)
            {
                _downThrustFlame = thrusters[i];
            }
        }

        _characterController.OnBecomeGrounded += EndEmergencyBoost;
    }

    private void Update()
    {
        if (_characterController == null) return;

        bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
        bool canEmergencyBoost = _characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
        if (!canEmergencyBoost) EndEmergencyBoost();

        else if (Config.IsEmergencyBoostEnabled && isInputting && Time.time - _lastBoostInputTime < Config.EmergencyBoostInputTime && _jetpackController._resources.GetFuel() > 0f && !_isEmergencyBoosting)
        {
            ApplyEmergencyBoost();
        }

        if (isInputting && canEmergencyBoost) _lastBoostInputTime = Time.time;
        if (_isEmergencyBoosting) _jetpackModel._chargeSeconds = float.PositiveInfinity;
    }

    private void ApplyEmergencyBoost()
    {
        _isEmergencyBoosting = true;
        _lastBoostTime = Time.time;
        _jetpackModel._boostChargeFraction = 0f;
        _jetpackController._resources._currentFuel = Mathf.Max(0f, _jetpackController._resources.GetFuel() - Config.EmergencyBoostCost);
        float boostPower = Config.EmergencyBoostPower;

        // set player velocity
        Vector3 pointVelocity = _characterController._transform.InverseTransformDirection(_characterController._lastGroundBody.GetPointVelocity(_characterController._transform.position));
        Vector3 localVelocity = _characterController._transform.InverseTransformDirection(_characterController._owRigidbody.GetVelocity()) - pointVelocity;
        _characterController._owRigidbody.AddLocalVelocityChange(new Vector3(-localVelocity.x * 0.5f, boostPower - localVelocity.y * 0.75f, -localVelocity.z * 0.5f));

        // sound and visual effects
        _superBoostAudio.pitch = Random.Range(1.0f, 1.4f);
        _superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, Config.EmergencyBoostVolume * 0.75f);
        _helmetAnimator.OnInstantDamage(boostPower, InstantDamageType.Impact);
        NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "EMERGENCY BOOST ACTIVATED", 5f), false);

        // if camerashaker is installed and camera shake is enabled, do a camera shake
        if (Config.EmergencyBoostCameraShakeAmount > 0f)
        {
            Main.Instance.CameraShakerAPI?.ExplosionShake(strength: boostPower * Config.EmergencyBoostCameraShakeAmount);
        }

        Main.WriteLine($"[{nameof(EmergencyBoostController)}] Super-Boosted", MessageType.Debug);
    }

    private void EndEmergencyBoost()
    {
        _isEmergencyBoosting = false;
        _jetpackModel._chargeSeconds = _characterController.IsGrounded() ? _jetpackModel._chargeSecondsGround : _jetpackModel._chargeSecondsAir;
    }

    private void LateUpdate()
    {
        if (_characterController == null) return;

        float timeSinceBoost = Time.time - _lastBoostTime;
        float thrusterCurve = -Mathf.Pow(5f * timeSinceBoost - 1f, 2f) + 1f;
        float thrusterScale = Mathf.Max(15f * thrusterCurve, _downThrustFlame._currentScale);
        _downThrustFlame.transform.localScale = Vector3.one * thrusterScale;
        _downThrustFlame._light.range = _downThrustFlame._baseLightRadius * thrusterScale;
        _downThrustFlame._thrusterRenderer.enabled = thrusterScale > 0f;
        _downThrustFlame._light.enabled = thrusterScale > 0f;
    }
}