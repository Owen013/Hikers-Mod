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
        if (Config.isFloatyPhysicsEnabled)
        {
            if (_characterController.IsGrounded() && !_characterController.IsSlidingOnIce())
            {
                float currentGravity = _characterController.GetNormalAccelerationScalar() / 12f;
                float maxGravity = Config.floatyPhysicsMaxGravity;
                float minGravity = Config.floatyPhysicsMinGravity;
                _characterController._acceleration = Mathf.Lerp(Config.floatyPhysicsMinAccel, Config.groundAccel, Mathf.Clamp((currentGravity - minGravity) / (maxGravity - minGravity), 0f, 1f));
            }
            else
            {
                _characterController._acceleration = Config.groundAccel;
            }
        }
    }
}