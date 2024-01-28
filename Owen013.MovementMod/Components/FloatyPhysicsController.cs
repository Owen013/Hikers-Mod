using UnityEngine;

namespace HikersMod.Components;

public class FloatyPhysicsController : MonoBehaviour
{
    private PlayerCharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
    }

    private void Update()
    {
        if (Config.IsFloatyPhysicsEnabled) UpdateAcceleration();
    }

    private void UpdateAcceleration()
    {
        if (_characterController == null) return;
        if (_characterController.IsGrounded() && !_characterController.IsSlidingOnIce())
        {
            float currentGravity = _characterController.GetNormalAccelerationScalar() / 12;
            float maxGravity = Config.FloatyPhysicsMaxGravity;
            float minGravity = Config.FloatyPhysicsMinGravity;
            _characterController._acceleration = Mathf.Lerp(Config.FloatyPhysicsMinAccel, Config.GroundAccel, Mathf.Clamp((currentGravity - minGravity) / (maxGravity - minGravity), 0f, 1f));
        }
        else
        {
            _characterController._acceleration = Config.GroundAccel;
        }
    }
}