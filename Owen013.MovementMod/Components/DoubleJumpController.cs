using OWML.Common;
using UnityEngine;

namespace HikersMod.Components;

public class DoubleJumpController : MonoBehaviour
{
    private PlayerCharacterController _characterController;

    private PlayerResources _resources;

    private PlayerCameraController _cameraController;

    private OWAudioSource _doubleJumpAudio;

    private bool _hasDoubleJumped;

    private float _doubleJumpCost = 2f;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _resources = GetComponent<PlayerResources>();
        _cameraController = GetComponentInChildren<PlayerCameraController>();

        // create audio source
        _doubleJumpAudio = new GameObject("HikersMod_DoubleJumpAudioSrc").AddComponent<OWAudioSource>();
        _doubleJumpAudio.transform.parent = GetComponentInChildren<PlayerAnimController>().transform;
        _doubleJumpAudio.transform.localPosition = new Vector3(0, 0, -1f);

        _characterController.OnBecomeGrounded += () =>
        {
            _hasDoubleJumped = false;
        };
    }

    private void OnDestroy()
    {
        Destroy(_doubleJumpAudio);
    }

    private void LateUpdate()
    {
        bool isInputting = !_characterController.IsGrounded() && OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
        if (!_characterController._isWearingSuit || PlayerState.InZeroG() || PlayerState.IsInsideShip() || PlayerState.IsCameraUnderwater())
        {
            _hasDoubleJumped = false;
        }
        else if (/*is enabled &&*/ isInputting && Time.time - WallJumpController.Instance.LastWallJumpTime > 0.25f && _resources.GetFuel() > 0f && !_hasDoubleJumped)
        {
            _hasDoubleJumped = true;
            _resources._currentFuel = Mathf.Max(0f, _resources.GetFuel() - _doubleJumpCost);
            _characterController._owRigidbody.AddLocalVelocityChange(new Vector3(0f, _characterController._maxJumpSpeed, 0f));

            // sound effect
            _doubleJumpAudio.pitch = Random.Range(2.0f, 3.8f);
            _doubleJumpAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, 0.5f);

            ModMain.Instance.WriteLine($"[{nameof(DirectionalBoostController)}] Directional-Boosted", MessageType.Debug);
        }
    }
}