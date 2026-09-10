using UnityEngine;

namespace HikersMod.Components;

public class PlayerSettingsController : MonoBehaviour
{
    PlayerCharacterController _playerController;

    JetpackThrusterModel _jetpackThrusterModel;

    void SetPlayerSettings()
    {
        // Change built-in character attributes
        _playerController._useChargeJump = Config.UseChargeJump;
        if (!Config.IsFloatyPhysicsEnabled)
            _playerController._acceleration = Config.GroundAccel;
        _playerController._runSpeed = Config.RunSpeed;
        _playerController._strafeSpeed = Config.StrafeSpeed;
        _playerController._walkSpeed = Config.WalkSpeed;
        _playerController._airSpeed = Config.AirSpeed;
        _playerController._airAcceleration = Config.AirAccel;
        _playerController._minJumpSpeed = Config.MinJumpPower;
        _playerController._maxJumpSpeed = Config.MaxJumpPower;
        _jetpackThrusterModel._maxTranslationalThrust = Config.JetpackAccel;
        _jetpackThrusterModel._boostThrust = Config.JetpackBoostAccel;
        _jetpackThrusterModel._boostSeconds = Config.JetpackBoostTime;
        PlayerResources._maxFuel = Config.MaxJetpackFuel;
    }

    void Awake()
    {
        _playerController = GetComponent<PlayerCharacterController>();
        _jetpackThrusterModel = FindObjectOfType<JetpackThrusterModel>();

        Config.OnConfigure += SetPlayerSettings;
        SetPlayerSettings();
    }

    void OnDestroy()
    {
        Config.OnConfigure -= SetPlayerSettings;
    }
}