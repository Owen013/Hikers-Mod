using UnityEngine;

namespace HikersMod.Components;

internal class CharacterAttributeController : MonoBehaviour
{
    private PlayerCharacterController _characterController;
    private JetpackThrusterModel _jetpackModel;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackModel = FindObjectOfType<JetpackThrusterModel>();

        Main.Instance.OnConfigure += ApplyChanges;
        ApplyChanges();
    }

    private void ApplyChanges()
    {
        // Change built-in character attributes
        _characterController._useChargeJump = Config.JumpStyle == "Charge";
        if (!Config.IsFloatyPhysicsEnabled) _characterController._acceleration = Config.GroundAccel;
        _characterController._airSpeed = Config.AirSpeed;
        _characterController._airAcceleration = Config.AirAccel;
        _characterController._minJumpSpeed = Config.MinJumpPower;
        _characterController._maxJumpSpeed = Config.MaxJumpPower;
        _jetpackModel._maxTranslationalThrust = Config.JetpackAccel;
        _jetpackModel._boostThrust = Config.JetpackBoostAccel;
        _jetpackModel._boostSeconds = Config.JetpackBoostTime;
    }
}