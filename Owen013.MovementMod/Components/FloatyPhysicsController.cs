using UnityEngine;

namespace HikersMod.Components;

public class FloatyPhysicsController : MonoBehaviour
{
    private PlayerCharacterController _playerCharacter;

    private void OnConfigure()
    {
        enabled = Config.IsFloatyPhysicsEnabled;
    }

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacterController>();

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void Update()
    {
        if (_playerCharacter.IsGrounded() && !_playerCharacter.IsSlidingOnIce())
        {
            float currentGravity = _playerCharacter.GetNormalAccelerationScalar() / 12f;
            float maxGravity = Config.FloatyPhysicsMaxGravity;
            float minGravity = Config.FloatyPhysicsMinGravity;
            _playerCharacter._acceleration = Mathf.Lerp(Config.FloatyPhysicsMinAccel, Config.GroundAccel, Mathf.Clamp01((currentGravity - minGravity) / (maxGravity - minGravity)));
        }
        else
        {
            _playerCharacter._acceleration = Config.GroundAccel;
        }
    }

    private void OnDisable()
    {
        _playerCharacter._acceleration = Config.GroundAccel;
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}