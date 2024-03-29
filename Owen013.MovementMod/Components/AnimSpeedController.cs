﻿using UnityEngine;

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
        float speedMultiplier = Mathf.Pow(_characterController.GetRelativeGroundVelocity().magnitude / 6f, 0.5f);
        if (Main.Instance.SmolHatchlingAPI != null)
        {
            speedMultiplier *= Main.Instance.SmolHatchlingAPI.GetAnimSpeed();
        }
        float gravMultiplier = Mathf.Sqrt(_characterController._acceleration / Config.groundAccel);

        _animator.speed = _characterController.IsGrounded() ? Mathf.Max(speedMultiplier * gravMultiplier, gravMultiplier) : 1f;
    }
}