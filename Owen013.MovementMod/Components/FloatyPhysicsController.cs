using UnityEngine;

namespace HikersMod.Components;

public class FloatyPhysicsController : MonoBehaviour
{
    public static FloatyPhysicsController Instance;
    private PlayerCharacterController _characterController;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _characterController = GetComponent<PlayerCharacterController>();
    }

    private void Update()
    {
        if (Main.Instance.isFloatyPhysicsEnabled) UpdateAcceleration();
    }

    private void UpdateAcceleration()
    {
        if (_characterController == null) return;
        float gravMultiplier = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce() ? Mathf.Clamp(Mathf.Pow(_characterController.GetNormalAccelerationScalar() / 12, Main.Instance.floatyPhysicsPower), 0.25f, 1f) : 1f;
        _characterController._acceleration = Main.Instance.groundAccel * gravMultiplier;
    }
}