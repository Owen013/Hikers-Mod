using UnityEngine;

namespace HikersMod.Components;

class AnimSpeedController : MonoBehaviour
{
    Animator _animator;

    PlayerCharacterController _playerController;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerController = Locator.GetPlayerController();
    }

    void LateUpdate()
    {
        // change viewbob strength quickly if on ground
        Vector3 groundVel = _playerController.GetRelativeGroundVelocity();
        groundVel.y = 0f;
        if (Mathf.Abs(groundVel.x) < 0.05f)
            groundVel.x = 0f;
        if (Mathf.Abs(groundVel.z) < 0.05f)
            groundVel.z = 0f;
        float groundSpeed = groundVel.magnitude;
        groundSpeed *= ModMain.SmolHatchlingAPI?.GetPlayerAnimSpeed() ?? 1f;
        float animSpeedMultiplier = Mathf.Sqrt(groundSpeed / 6);
        float floatyPhysicsMultiplier = Mathf.Sqrt(_playerController._acceleration / Config.GroundAccel);

        _animator.speed = Mathf.Max(animSpeedMultiplier * floatyPhysicsMultiplier, floatyPhysicsMultiplier);
    }
}