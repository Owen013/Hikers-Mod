using UnityEngine;

namespace HikersMod.Components;

class PlayerSettingsController : MonoBehaviour
{
    PlayerCharacterController _characterController;

    JetpackThrusterModel _jetpackModel;

    void SetPlayerSettings()
    {
        // Change built-in character attributes
        _characterController._useChargeJump = Config.UseChargeJump;
        if (!Config.IsFloatyPhysicsEnabled)
        {
            _characterController._acceleration = Config.GroundAccel;
        }
        _characterController._runSpeed = Config.RunSpeed;
        _characterController._strafeSpeed = Config.StrafeSpeed;
        _characterController._walkSpeed = Config.WalkSpeed;
        _characterController._airSpeed = Config.AirSpeed;
        _characterController._airAcceleration = Config.AirAccel;
        _characterController._minJumpSpeed = Config.MinJumpPower;
        _characterController._maxJumpSpeed = Config.MaxJumpPower;
        _jetpackModel._maxTranslationalThrust = Config.JetpackAccel;
        _jetpackModel._boostThrust = Config.JetpackBoostAccel;
        _jetpackModel._boostSeconds = Config.JetpackBoostTime;
        PlayerResources._maxFuel = Config.MaxJetpackFuel;
    }

    void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackModel = FindObjectOfType<JetpackThrusterModel>();

        Config.OnConfigure += SetPlayerSettings;
        SetPlayerSettings();
    }

    void OnDestroy()
    {
        Config.OnConfigure -= SetPlayerSettings;
    }
}