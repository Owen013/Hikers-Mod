using UnityEngine;

namespace HikersMod.Components;

public class FloatyPhysicsController : MonoBehaviour
{
    private PlayerCharacterController _playerController;

    private void OnConfigure()
    {
        enabled = Config.IsFloatyPhysicsEnabled;
    }

    private void Awake()
    {
        _playerController = GetComponent<PlayerCharacterController>();

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void Update()
    {
        if (_playerController.IsGrounded() && !_playerController.IsSlidingOnIce())
        {
            float currentGravity = _playerController.GetNormalAccelerationScalar() / 12f;
            float maxGravity = Config.FloatyPhysicsMaxGravity;
            float minGravity = Config.FloatyPhysicsMinGravity;
            _playerController._acceleration = Mathf.Lerp(Config.FloatyPhysicsMinAccel, Config.GroundAccel, Mathf.Clamp01((currentGravity - minGravity) / (maxGravity - minGravity)));
        }
        else
        {
            _playerController._acceleration = Config.GroundAccel;
        }
    }

    private void OnDisable()
    {
        _playerController._acceleration = Config.GroundAccel;
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}