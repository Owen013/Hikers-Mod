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

        ModMain.Instance.OnConfigure += ApplyChanges;
        ApplyChanges();
    }

    private void OnDestroy()
    {
        ModMain.Instance.OnConfigure -= ApplyChanges;
    }

    private void ApplyChanges()
    {
        // Change built-in character attributes
        _characterController._useChargeJump = ModMain.Instance.UseChargeJump;
        if (!ModMain.Instance.IsFloatyPhysicsEnabled) _characterController._acceleration = ModMain.Instance.GroundAccel;
        _characterController._airSpeed = ModMain.Instance.AirSpeed;
        _characterController._airAcceleration = ModMain.Instance.AirAccel;
        _characterController._minJumpSpeed = ModMain.Instance.MinJumpPower;
        _characterController._maxJumpSpeed = ModMain.Instance.MaxJumpPower;
        _jetpackModel._maxTranslationalThrust = ModMain.Instance.JetpackAccel;
        _jetpackModel._boostThrust = ModMain.Instance.JetpackBoostAccel;
        _jetpackModel._boostSeconds = ModMain.Instance.JetpackBoostTime;
        PlayerResources._maxFuel = ModMain.Instance.MaxJetpackFuel;
    }
}