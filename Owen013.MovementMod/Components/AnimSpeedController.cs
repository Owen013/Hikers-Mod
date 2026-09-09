using UnityEngine;

namespace HikersMod.Components;

class AnimSpeedController : MonoBehaviour
{
    Animator _animator;

    PlayerCharacterController _characterController;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _characterController = Locator.GetPlayerController();
    }

    void LateUpdate()
    {
        // change viewbob strength quickly if on ground
        Vector3 groundVel = _characterController.GetRelativeGroundVelocity();
        groundVel.y = 0f;
        if (Mathf.Abs(groundVel.x) < 0.05f)
        {
            groundVel.x = 0f;
        }
        if (Mathf.Abs(groundVel.z) < 0.05f)
        {
            groundVel.z = 0f;
        }
        float groundSpeed = groundVel.magnitude;
        if (ModMain.SmolHatchlingAPI != null)
        {
            groundSpeed *= ModMain.SmolHatchlingAPI.GetPlayerAnimSpeed();
        }
        float animSpeedMultiplier = Mathf.Sqrt(groundSpeed / 6);
        float floatyPhysicsMultiplier = Mathf.Sqrt(_characterController._acceleration / Config.GroundAccel);

        _animator.speed = Mathf.Max(animSpeedMultiplier * floatyPhysicsMultiplier, floatyPhysicsMultiplier);
    }
}