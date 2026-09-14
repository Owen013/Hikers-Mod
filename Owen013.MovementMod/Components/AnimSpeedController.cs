using UnityEngine;
using static HikersMod.ModMain;

namespace HikersMod.Components;

public class AnimSpeedController : MonoBehaviour
{
    private Animator _animator;

    private PlayerCharacterController _playerCharacter;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerCharacter = Locator.GetPlayerController();
    }

    private void LateUpdate()
    {
        // change viewbob strength quickly if on ground
        Vector3 groundVel = _playerCharacter.GetRelativeGroundVelocity();
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
        groundSpeed *= SmolHatchlingAPI?.GetPlayerAnimSpeed() ?? 1f;
        float animSpeedMultiplier = Mathf.Sqrt(groundSpeed / 6);
        float floatyPhysicsMultiplier = Mathf.Sqrt(_playerCharacter._acceleration / Config.GroundAccel);

        _animator.speed = Mathf.Max(animSpeedMultiplier * floatyPhysicsMultiplier, floatyPhysicsMultiplier);
    }
}