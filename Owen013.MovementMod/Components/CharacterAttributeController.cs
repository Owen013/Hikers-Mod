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

    private void OnDestroy()
    {
        Main.Instance.OnConfigure -= ApplyChanges;
    }

    private void ApplyChanges()
    {
        // Change built-in character attributes
        _characterController._useChargeJump = Config.useChargeJump;
        if (!Config.isFloatyPhysicsEnabled) _characterController._acceleration = Config.groundAccel;
        _characterController._airSpeed = Config.airSpeed;
        _characterController._airAcceleration = Config.airAccel;
        _characterController._minJumpSpeed = Config.minJumpPower;
        _characterController._maxJumpSpeed = Config.maxJumpPower;
        _jetpackModel._maxTranslationalThrust = Config.jetpackAccel;
        _jetpackModel._boostThrust = Config.jetpackBoostAccel;
        _jetpackModel._boostSeconds = Config.jetpackBoostTime;
    }
}