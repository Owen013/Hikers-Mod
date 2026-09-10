using UnityEngine;

namespace HikersMod.Components;

public class FloatyPhysicsController : MonoBehaviour
{
    PlayerCharacterController _playerController;

    void OnConfigure()
    {
        enabled = Config.IsFloatyPhysicsEnabled;
    }

    void Awake()
    {
        _playerController = GetComponent<PlayerCharacterController>();

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    void Update()
    {
        if (_playerController.IsGrounded() && !_playerController.IsSlidingOnIce())
        {
            float currentGravity = _playerController.GetNormalAccelerationScalar() / 12f;
            float maxGravity = Config.FloatyPhysicsMaxGravity;
            float minGravity = Config.FloatyPhysicsMinGravity;
            _playerController._acceleration = Mathf.Lerp(Config.FloatyPhysicsMinAccel, Config.GroundAccel, Mathf.Clamp01((currentGravity - minGravity) / (maxGravity - minGravity)));
        }
        else
            _playerController._acceleration = Config.GroundAccel;
    }

    void OnDisable()
    {
        _playerController._acceleration = Config.GroundAccel;
    }

    void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}