using UnityEngine;

namespace HikersMod.Components;

public class AnimSpeedController : MonoBehaviour
{
    private Animator _animator;
    private PlayerCharacterController _characterController;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _characterController = Locator.GetPlayerController();
    }

    private void Update()
    {
        float animSpeedMultiplier = Mathf.Pow(_characterController.GetRelativeGroundVelocity().magnitude / 6f, 0.5f);
        if (Main.Instance.SmolHatchlingAPI != null)
        {
            animSpeedMultiplier *= Main.Instance.SmolHatchlingAPI.GetAnimSpeed();
        }
        float floatyPhysicsMultiplier = Mathf.Sqrt(_characterController._acceleration / Config.GroundAccel);
        float underwaterMultiplier = Main.Instance.ImmersionAPI != null ? Main.Instance.ImmersionAPI.GetAnimSpeed() : 1f;

        _animator.speed = _characterController.IsGrounded() ? Mathf.Max(animSpeedMultiplier * floatyPhysicsMultiplier, floatyPhysicsMultiplier) : underwaterMultiplier;
    }
}