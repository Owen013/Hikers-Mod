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
        if (Main.Instance.IsFloatyPhysicsEnabled) UpdateAcceleration();
    }

    private void UpdateAcceleration()
    {
        if (_characterController == null) return;
        if (_characterController.IsGrounded() && !_characterController.IsSlidingOnIce())
        {
            float currentGravity = _characterController.GetNormalAccelerationScalar() / 12;
            float maxGravity = Main.Instance.FloatyPhysicsMaxGravity;
            float minGravity = Main.Instance.FloatyPhysicsMinGravity;
            _characterController._acceleration = Mathf.Lerp(Main.Instance.FloatyPhysicsMinAccel, Main.Instance.GroundAccel, Mathf.Clamp((currentGravity - minGravity) / (maxGravity - minGravity), 0f, 1f));
        }
        else
        {
            _characterController._acceleration = Main.Instance.GroundAccel;
        }
    }
}