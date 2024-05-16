using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class DirectionalBoostController : MonoBehaviour
{
    private PlayerCharacterController _characterController;

    private PlayerResources _resources;

    private PlayerCameraController _cameraController;

    private OWAudioSource _directionalBoostAudio;

    private float _lastDirectionalBoostTime;

    private bool _hasBoosted;

    private float _boostPower = 10f;

    private float _boostCost = 2f;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _resources = GetComponent<PlayerResources>();
        _cameraController = GetComponentInChildren<PlayerCameraController>();

        // create audio source
        _directionalBoostAudio = new GameObject("HikersMod_DirectionalBoostAudioSrc").AddComponent<OWAudioSource>();
        _directionalBoostAudio.transform.parent = GetComponentInChildren<PlayerAnimController>().transform;
        _directionalBoostAudio.transform.localPosition = new Vector3(0, 0, -1f);

        _characterController.OnBecomeGrounded += () =>
        {
            _hasBoosted = false;
        };
    }

    private void OnDestroy()
    {
        Destroy(_directionalBoostAudio);
    }

    private void LateUpdate()
    {
        bool isInputting = !_characterController.IsGrounded() && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
        if (!_characterController._isWearingSuit || PlayerState.InZeroG() || PlayerState.IsInsideShip() || PlayerState.IsCameraUnderwater())
        {
            _hasBoosted = false;
        }
        else if (/*is enabled &&*/ isInputting && Time.time - WallJumpController.Instance.LastWallJumpTime > 0.25f && _resources.GetFuel() > 0f && !_hasBoosted)
        {
            _hasBoosted = true;
            _lastDirectionalBoostTime = Time.time;
            _resources._currentFuel = Mathf.Max(0f, _resources.GetFuel() - _boostCost);
            float boostPower = _boostPower;

            // set player velocity
            Vector3 pointVelocity = _characterController._transform.InverseTransformDirection(_characterController._lastGroundBody.GetPointVelocity(_characterController._transform.position));
            Vector3 localVelocity = _characterController._transform.InverseTransformDirection(_characterController._owRigidbody.GetVelocity()) - pointVelocity;
            _characterController._owRigidbody.AddVelocityChange(_cameraController.transform.forward * _boostPower);

            // sound effect
            _directionalBoostAudio.pitch = Random.Range(2.0f, 3.8f);
            _directionalBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, 0.5f);

            ModMain.Instance.WriteLine($"[{nameof(DirectionalBoostController)}] Directional-Boosted", MessageType.Debug);
        }
    }
}