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
        if (ModMain.IsFloatyPhysicsEnabled)
        {
            if (_characterController.IsGrounded() && !_characterController.IsSlidingOnIce())
            {
                float currentGravity = _characterController.GetNormalAccelerationScalar() / 12f;
                float maxGravity = ModMain.FloatyPhysicsMaxGravity;
                float minGravity = ModMain.FloatyPhysicsMinGravity;
                _characterController._acceleration = Mathf.Lerp(ModMain.FloatyPhysicsMinAccel, ModMain.GroundAccel, Mathf.Clamp((currentGravity - minGravity) / (maxGravity - minGravity), 0f, 1f));
            }
            else
            {
                _characterController._acceleration = ModMain.GroundAccel;
            }
        }
    }
}