using UnityEngine;

namespace HikersMod.Components;

class FloatyPhysicsController : MonoBehaviour
{
    PlayerCharacterController _characterController;

    void OnConfigure()
    {
        enabled = Config.IsFloatyPhysicsEnabled;
    }

    void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    void Update()
    {
        if (_characterController.IsGrounded() && !_characterController.IsSlidingOnIce())
        {
            float currentGravity = _characterController.GetNormalAccelerationScalar() / 12f;
            float maxGravity = Config.FloatyPhysicsMaxGravity;
            float minGravity = Config.FloatyPhysicsMinGravity;
            _characterController._acceleration = Mathf.Lerp(Config.FloatyPhysicsMinAccel, Config.GroundAccel, Mathf.Clamp01((currentGravity - minGravity) / (maxGravity - minGravity)));
        }
        else
        {
            _characterController._acceleration = Config.GroundAccel;
        }
    }

    void OnDisable()
    {
        _characterController._acceleration = Config.GroundAccel;
    }

    void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}