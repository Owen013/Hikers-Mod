using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components
{
    public class SuperBoostController : MonoBehaviour
    {
        public static SuperBoostController s_instance;
        public float _lastBoostInputTime;
        public float _lastBoostTime;
        public float _lastBoostPower;
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
            s_instance = this;
            Harmony.CreateAndPatchAll(typeof(SuperBoostController));
        }

        public void Update()
        {
            if (!_characterController) return;
            bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump, InputMode.Character) && !OWInput.IsPressed(InputLibrary.thrustUp, InputMode.Character);
            bool meetsCriteria = _characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
            if (!meetsCriteria) _isSuperBoosting = false;
            else if (isInputting && meetsCriteria && _jetpackController._resources.GetFuel() > 0f && Time.time - _lastBoostInputTime < 0.25f && HikersMod.s_instance._isSuperBoostEnabled && !_isSuperBoosting)
            {
                // Apply superboost
                _lastBoostTime = Time.time;
                _isSuperBoosting = true;
                _jetpackModel._boostChargeFraction = 0f;
                float powerPercent = Mathf.Min(HikersMod.s_instance._superBoostCost, _jetpackController._resources.GetFuel()) / HikersMod.s_instance._superBoostCost;
                float boostPower = HikersMod.s_instance._superBoostPower * powerPercent;
                _lastBoostPower = boostPower;
                float boostCost = HikersMod.s_instance._superBoostCost * powerPercent;
                _jetpackController._resources._currentFuel -= boostCost;
                Vector3 pointVelocity = _characterController._transform.InverseTransformDirection(_characterController._lastGroundBody.GetPointVelocity(_characterController._transform.position));
                Vector3 localVelocity = _characterController._transform.InverseTransformDirection(_characterController._owRigidbody.GetVelocity()) - pointVelocity;
                _characterController._owRigidbody.AddLocalVelocityChange(new Vector3(-localVelocity.x * 0.5f, boostPower - localVelocity.y * 0.5f, -localVelocity.z * 0.5f));
                _superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, Mathf.Min(boostPower * 0.05f, 50));
                _helmetAnimator.OnInstantDamage(boostPower, InstantDamageType.Impact);
                NotificationManager.s_instance.PostNotification(new NotificationData(NotificationTarget.Player, "EMERGENCY BOOST ACTIVATED", 5f), false);
                HikersMod.s_instance.DebugLog("Super-Boosted");
            }
            if (isInputting && meetsCriteria) _lastBoostInputTime = Time.time;
            if (_isSuperBoosting)
            {
                _jetpackModel._chargeSeconds = float.PositiveInfinity;
                float thrusterScale = Mathf.Max(_downThrustFlame._currentScale, Mathf.Pow(Mathf.Max(1 - (Time.time - _lastBoostTime), 0), 2) * _lastBoostPower);
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
        public static void OnCharacterControllerStart()
        {
            s_instance._characterController = FindObjectOfType<PlayerCharacterController>();
            s_instance._audioController = FindObjectOfType<PlayerAudioController>();
            s_instance._jetpackModel = FindObjectOfType<JetpackThrusterModel>();
            s_instance._jetpackController = FindObjectOfType<JetpackThrusterController>();
            s_instance._helmetAnimator = FindObjectOfType<HUDHelmetAnimator>();
            s_instance._superBoostAudio = new GameObject("HikersMod_SuperBoostAudioSrc").AddComponent<OWAudioSource>();
            s_instance._superBoostAudio.transform.parent = s_instance._audioController.transform;
            s_instance._superBoostAudio.transform.localPosition = new Vector3(0, 0, 1);
            var thrusters = Resources.FindObjectsOfTypeAll<ThrusterFlameController>();
            for (int i = 0; i < thrusters.Length; i++) if (thrusters[i]._thruster == Thruster.Up_LeftThruster) s_instance._downThrustFlame = thrusters[i];
            s_instance._characterController.OnBecomeGrounded += () => s_instance._isSuperBoosting = false;
        }
    }
}
