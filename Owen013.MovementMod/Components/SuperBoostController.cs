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
        public HUDHelmetAnimator _helmetAnimator;

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(SuperBoostController));
        }

        public void Update()
        {
            if (!_characterController) return;
            bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
            bool meetsCriteria = _characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
            if (!meetsCriteria) _isSuperBoosting = false;
            else if (isInputting && meetsCriteria && _jetpackController._resources.GetFuel() > 0f && Time.time - _lastBoostInputTime < 0.25f && HikersMod.Instance._isSuperBoostEnabled && !_isSuperBoosting)
            {
                _lastBoostTime = Time.time;
                _isSuperBoosting = true;
                _jetpackModel._boostChargeFraction = 0f;
                _jetpackController._resources._currentFuel -= HikersMod.Instance._superBoostCost;
                Vector3 pointVelocity = _characterController._transform.InverseTransformDirection(_characterController._lastGroundBody.GetPointVelocity(_characterController._transform.position));
                Vector3 localVelocity = _characterController._transform.InverseTransformDirection(_characterController._owRigidbody.GetVelocity()) - pointVelocity;
                _characterController._owRigidbody.AddLocalVelocityChange(new Vector3(0f, HikersMod.Instance._superBoostPower - localVelocity.y, 0f));
                _superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, 1f);
                _helmetAnimator.OnInstantDamage(10f, InstantDamageType.Impact);
                NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "EMERGENCY BOOST ACTIVATED", 5f), false);
                HikersMod.Instance.DebugLog("Super-Boosted");
            }
            if (isInputting && meetsCriteria) _lastBoostInputTime = Time.time;
            if (_isSuperBoosting)
            {
                _jetpackModel._chargeSeconds = float.PositiveInfinity;
                float thrusterScale = Mathf.Max(_downThrustFlame._currentScale, Mathf.Max(1 - (Time.time - _lastBoostTime), 0) * 10f);
                _downThrustFlame.transform.localScale = Vector3.one * thrusterScale;
                _downThrustFlame._light.range = _downThrustFlame._baseLightRadius * thrusterScale;
                _downThrustFlame._thrusterRenderer.enabled = (thrusterScale > 0f);
                _downThrustFlame._light.enabled = (thrusterScale > 0f);
            }
            else
            {
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
            Instance._helmetAnimator = FindObjectOfType<HUDHelmetAnimator>();
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
