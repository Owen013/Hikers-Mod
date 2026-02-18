using UnityEngine;

namespace HikersMod.Components;

public class FloatyPhysicsController : MonoBehaviour
{
    private PlayerCharacterController _characterController;

    private void OnConfigure()
    {
        base.enabled = Config.IsFloatyPhysicsEnabled;
    }

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void Update()
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

    private void OnDisable()
    {
        _characterController._acceleration = Config.GroundAccel;
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}