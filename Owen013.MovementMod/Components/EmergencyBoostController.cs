using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class EmergencyBoostController : MonoBehaviour
{
    private PlayerCharacterController _characterController;

    private JetpackThrusterController _jetpackController;

    private JetpackThrusterModel _jetpackModel;

    private OWAudioSource _emergencyBoostAudio;

    private ThrusterFlameController _downThrustFlame;

    private HUDHelmetAnimator _helmetAnimator;

    private float _lastEmergencyBoostInputTime;

    private float _lastEmergencyBoostTime;

    private bool _isEmergencyBoosting;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackModel = GetComponent<JetpackThrusterModel>();
        _jetpackController = GetComponent<JetpackThrusterController>();
        _helmetAnimator = GetComponentInChildren<HUDHelmetAnimator>();

        // create super boost audio source
        _emergencyBoostAudio = new GameObject("HikersMod_EmergencyBoostAudioSrc").AddComponent<OWAudioSource>();
        _emergencyBoostAudio.transform.parent = GetComponentInChildren<PlayerAudioController>().transform;
        _emergencyBoostAudio.transform.localPosition = new Vector3(0, -1f, 1f);

        // get player's downward thruster flame
        var thrusters = _characterController.gameObject.GetComponentsInChildren<ThrusterFlameController>(includeInactive: true);
        foreach (ThrusterFlameController thruster in thrusters)
        {
            if (thruster._thruster == Thruster.Up_LeftThruster)
            {
                _downThrustFlame = thruster;
                break;
            }
        }

        _characterController.OnBecomeGrounded += EndEmergencyBoost;
    }

    private void OnDestroy()
    {
        Destroy(_emergencyBoostAudio);
    }

    private void LateUpdate()
    {
        bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
        bool canEmergencyBoost = _characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
        if (!canEmergencyBoost)
        {
            EndEmergencyBoost();
        }
        else if (ModMain.Instance.IsEmergencyBoostEnabled && isInputting && Time.time - _lastEmergencyBoostInputTime < ModMain.Instance.EmergencyBoostInputTime && Time.time - WallJumpController.Instance.LastWallJumpTime > 0.5f && _jetpackController._resources.GetFuel() > 0f && !_isEmergencyBoosting)
        {
            ApplyEmergencyBoost();
        }

        if (isInputting && canEmergencyBoost) _lastEmergencyBoostInputTime = Time.time;
        if (_isEmergencyBoosting) _jetpackModel._chargeSeconds = float.PositiveInfinity;

        float timeSinceBoost = Time.time - _lastEmergencyBoostTime;
        float thrusterCurve = -Mathf.Pow(5f * timeSinceBoost - 1f, 2f) + 1f;
        float thrusterScale = Mathf.Max(15f * thrusterCurve, _downThrustFlame._currentScale);
        _downThrustFlame.transform.localScale = Vector3.one * thrusterScale;
        _downThrustFlame._light.range = _downThrustFlame._baseLightRadius * thrusterScale;
        _downThrustFlame._thrusterRenderer.enabled = thrusterScale > 0f;
        _downThrustFlame._light.enabled = thrusterScale > 0f;
    }

    private void ApplyEmergencyBoost()
    {
        _isEmergencyBoosting = true;
        _lastEmergencyBoostTime = Time.time;
        _jetpackModel._boostChargeFraction = 0f;
        _jetpackController._resources._currentFuel = Mathf.Max(0f, _jetpackController._resources.GetFuel() - ModMain.Instance.EmergencyBoostCost);
        float boostPower = ModMain.Instance.EmergencyBoostPower;

        // set player velocity
        Vector3 pointVelocity = _characterController._transform.InverseTransformDirection(_characterController._lastGroundBody.GetPointVelocity(_characterController._transform.position));
        Vector3 localVelocity = _characterController._transform.InverseTransformDirection(_characterController._owRigidbody.GetVelocity()) - pointVelocity;
        _characterController._owRigidbody.AddLocalVelocityChange(new Vector3(-localVelocity.x * 0.5f, boostPower - localVelocity.y * 0.5f, -localVelocity.z * 0.5f));

        // sound and visual effects
        _emergencyBoostAudio.pitch = Random.Range(1.0f, 1.4f);
        _emergencyBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, ModMain.Instance.EmergencyBoostVolume * 0.75f);
        _helmetAnimator.OnInstantDamage(boostPower, InstantDamageType.Impact);
        NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "EMERGENCY BOOST ACTIVATED", 5f), false);

        // if camerashaker is installed and camera shake is enabled, do a camera shake
        if (ModMain.Instance.EmergencyBoostCameraShakeAmount > 0f)
        {
            ModMain.Instance.CameraShakerAPI?.ExplosionShake(strength: boostPower * ModMain.Instance.EmergencyBoostCameraShakeAmount);
        }

        ModMain.Instance.WriteLine($"[{nameof(EmergencyBoostController)}] Super-Boosted", MessageType.Debug);
    }

    private void EndEmergencyBoost()
    {
        _isEmergencyBoosting = false;
        _jetpackModel._chargeSeconds = _characterController.IsGrounded() ? _jetpackModel._chargeSecondsGround : _jetpackModel._chargeSecondsAir;
    }
}