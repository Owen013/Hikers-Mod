using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class SuperBoostController : MonoBehaviour
    {
        public static SuperBoostController Instance;
        public float _lastBoostInputTime;
        public float _lastBoostTime;
        public bool _isSuperBoosting;
        public OWAudioSource _superBoostAudio;
        public JetpackThrusterModel _jetpackModel;
        public JetpackThrusterController _jetpackController;
        public PlayerCharacterController _characterController;
        public PlayerAudioController _audioController;
        public ThrusterFlameController _downThrustFlame;

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(SuperBoostController));
        }

        public void Update()
        {
            if (!_characterController)
            {
                HikersMod.Instance.DebugLog("No character controller", OWML.Common.MessageType.Warning);
                return;
            }
            bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
            bool meetsCriteria = _characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
            if (!meetsCriteria) _isSuperBoosting = false;
            else if (isInputting && meetsCriteria && _jetpackController._resources.GetFuel() > 0 && Time.time - _lastBoostInputTime < 0.25f && HikersMod.Instance._isSuperBoostEnabled && !_isSuperBoosting)
            {
                _lastBoostTime = Time.time;
                _isSuperBoosting = true;
                _jetpackModel._boostChargeFraction = 1f;
                _superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, 1f);
                HikersMod.Instance.DebugLog("Super-Boosted");
            }
            if (isInputting && meetsCriteria) _lastBoostInputTime = Time.time;
            if (_isSuperBoosting)
            {
                if (_jetpackModel._boostChargeFraction > 0)
                {
                    _jetpackModel._boostActivated = true;
                    _jetpackController._translationalInput.y = 1;
                }
                else
                {
                    _jetpackController._translationalInput.y = 0;
                }
                _jetpackModel._boostThrust = HikersMod.Instance._jetpackBoostAccel * HikersMod.Instance._superBoostPower;
                _jetpackModel._boostSeconds = HikersMod.Instance._jetpackBoostTime / HikersMod.Instance._superBoostPower;
                _jetpackModel._chargeSeconds = float.PositiveInfinity;
                _downThrustFlame._currentScale = Mathf.Max(_downThrustFlame._currentScale, Mathf.Max(2 - (Time.time - _lastBoostTime), 0) * 7.5f * _jetpackModel._boostChargeFraction);
            }
            else
            {
                _jetpackModel._boostThrust = HikersMod.Instance._jetpackBoostAccel;
                _jetpackModel._boostSeconds = HikersMod.Instance._jetpackBoostTime;
                if (_characterController.IsGrounded()) _jetpackModel._chargeSeconds = _jetpackModel._chargeSecondsGround;
                else _jetpackModel._chargeSeconds = _jetpackModel._chargeSecondsAir;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void CharacterControllerStart()
        {
            Instance._characterController = FindObjectOfType<PlayerCharacterController>();
            Instance._audioController = FindObjectOfType<PlayerAudioController>();
            Instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
            Instance._jetpackController = FindObjectOfType<JetpackThrusterController>();
            Instance._superBoostAudio = new GameObject("HikersMod_SuperBoostAudioSrc").AddComponent<OWAudioSource>();
            Instance._superBoostAudio.transform.parent = Instance._audioController.transform;
            Instance._superBoostAudio.transform.localPosition = new Vector3(0, 0, 1);
            var thrusters = Resources.FindObjectsOfTypeAll<ThrusterFlameController>();
            for (int i = 0; i < thrusters.Length; i++) if (thrusters[i]._thruster == Thruster.Up_LeftThruster) Instance._downThrustFlame = thrusters[i];
            Instance._characterController.OnBecomeGrounded += () =>
            {
                Instance._isSuperBoosting = false;
            };
        }
    }
}
