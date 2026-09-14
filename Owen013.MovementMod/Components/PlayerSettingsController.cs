using UnityEngine;

namespace HikersMod.Components;

public class PlayerSettingsController : MonoBehaviour
{
    private PlayerCharacterController _playerCharacter;

    private JetpackThrusterModel _jetpackThrusterModel;

    private void SetPlayerSettings()
    {
        // Change built-in character attributes.
        _playerCharacter._useChargeJump = Config.UseChargeJump;
        if (!Config.IsFloatyPhysicsEnabled)
        {
            _playerCharacter._acceleration = Config.GroundAccel;
        }
        _playerCharacter._runSpeed = Config.RunSpeed;
        _playerCharacter._strafeSpeed = Config.StrafeSpeed;
        _playerCharacter._walkSpeed = Config.WalkSpeed;
        _playerCharacter._airSpeed = Config.AirSpeed;
        _playerCharacter._airAcceleration = Config.AirAccel;
        _playerCharacter._minJumpSpeed = Config.MinJumpPower;
        _playerCharacter._maxJumpSpeed = Config.MaxJumpPower;
        _jetpackThrusterModel._maxTranslationalThrust = Config.JetpackAccel;
        _jetpackThrusterModel._boostThrust = Config.JetpackBoostAccel;
        _jetpackThrusterModel._boostSeconds = Config.JetpackBoostTime;
        PlayerResources._maxFuel = Config.MaxJetpackFuel;
    }

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacterController>();
        _jetpackThrusterModel = FindObjectOfType<JetpackThrusterModel>();

        Config.OnConfigure += SetPlayerSettings;
        SetPlayerSettings();
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= SetPlayerSettings;
    }
}