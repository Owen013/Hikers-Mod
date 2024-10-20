using UnityEngine;

namespace HikersMod.Components;

public class CharacterAttributeController : MonoBehaviour
{
    private PlayerCharacterController _characterController;

    private JetpackThrusterModel _jetpackModel;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackModel = FindObjectOfType<JetpackThrusterModel>();

        ModMain.OnConfigure += ApplyChanges;
        ApplyChanges();
    }

    private void OnDestroy()
    {
        ModMain.OnConfigure -= ApplyChanges;
    }

    private void ApplyChanges()
    {
        // Change built-in character attributes
        _characterController._useChargeJump = ModMain.UseChargeJump;
        if (!ModMain.IsFloatyPhysicsEnabled) _characterController._acceleration = ModMain.GroundAccel;
        _characterController._airSpeed = ModMain.AirSpeed;
        _characterController._airAcceleration = ModMain.AirAccel;
        _characterController._minJumpSpeed = ModMain.MinJumpPower;
        _characterController._maxJumpSpeed = ModMain.MaxJumpPower;
        _jetpackModel._maxTranslationalThrust = ModMain.JetpackAccel;
        _jetpackModel._boostThrust = ModMain.JetpackBoostAccel;
        _jetpackModel._boostSeconds = ModMain.JetpackBoostTime;
        PlayerResources._maxFuel = ModMain.MaxJetpackFuel;
    }
}