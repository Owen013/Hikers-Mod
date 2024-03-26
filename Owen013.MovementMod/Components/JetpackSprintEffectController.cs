using UnityEngine;

namespace HikersMod.Components
{
    public class JetpackSprintEffectController : MonoBehaviour
    {
        private GameObject _playerSuit;
        private GameObject _playerJetpack;
        private JetpackThrusterAudio _jetpackAudio;
        private ThrusterFlameController[] _thrusters;
        private Vector2 _thrusterVector;

        private void Awake()
        {
            _jetpackAudio = GetComponentInChildren<JetpackThrusterAudio>();
            _thrusters = GetComponentsInChildren<ThrusterFlameController>(includeInactive: true);
            _playerSuit = GetComponentInChildren<PlayerAnimController>().transform.Find("Traveller_Mesh_v01:Traveller_Geo").gameObject;
            _playerJetpack = _playerSuit.transform.Find("Traveller_Mesh_v01:Props_HEA_Jetpack").gameObject;
            _thrusterVector = Vector2.zero;
        }

        private void LateUpdate()
        {
            bool jetpackVisible = _playerSuit.activeSelf && _playerJetpack.activeSelf;

            // get thruster vector IF the player is sprinting and the jetpack is visible. Otherwise, move towards zero
            _thrusterVector = Vector2.MoveTowards(_thrusterVector, SpeedController.Instance.IsSprintActive && jetpackVisible && Config.IsSprintEffectEnabled ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero, Time.deltaTime * 5);
            Vector2 flameVector = _thrusterVector;

            // clamp the vector so it doesn't become too big
            flameVector.x = Mathf.Clamp(flameVector.x, -20, 20);
            flameVector.y = Mathf.Clamp(flameVector.y, -20, 20);

            // update thruster sound, as long as it's not being set by the actual audio controller
            if (_jetpackAudio.isActiveAndEnabled == false)
            {
                float soundVolume = flameVector.magnitude;
                float soundPan = -flameVector.x * 0.4f;
                bool hasFuel = _jetpackAudio._playerResources.GetFuel() > 0f;
                bool isUnderwater = _jetpackAudio._underwater;
                _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._translationalSource, soundVolume, soundPan, !isUnderwater && hasFuel);
                _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._underwaterSource, soundVolume, soundPan, isUnderwater);
                _jetpackAudio.UpdateTranslationalSource(_jetpackAudio._oxygenSource, soundVolume, soundPan, !isUnderwater && !hasFuel);
            }

            // update thruster visuals as long as their controllers are inactive
            for (int i = 0; i < _thrusters.Length; i++)
            {
                if (_thrusters[i].isActiveAndEnabled) break;

                switch (_thrusters[i]._thruster)
                {
                    case Thruster.Forward_LeftThruster:
                        SetThrusterScale(_thrusters[i], flameVector.y);
                        break;
                    case Thruster.Forward_RightThruster:
                        SetThrusterScale(_thrusters[i], flameVector.y);
                        break;
                    case Thruster.Left_Thruster:
                        SetThrusterScale(_thrusters[i], -flameVector.x);
                        break;
                    case Thruster.Right_Thruster:
                        SetThrusterScale(_thrusters[i], flameVector.x);
                        break;
                    case Thruster.Backward_LeftThruster:
                        SetThrusterScale(_thrusters[i], -flameVector.y);
                        break;
                    case Thruster.Backward_RightThruster:
                        SetThrusterScale(_thrusters[i], -flameVector.y);
                        break;
                }
            }
        }

        private static void SetThrusterScale(ThrusterFlameController thruster, float thrusterScale)
        {
            if (thruster._underwater) thrusterScale = 0f;

            // reset scale spring if it's rly small so it doesn't bounce back up
            if (thruster._currentScale <= 0.001f)
            {
                thruster._currentScale = 0f;
                thruster._scaleSpring.ResetVelocity();
            }

            thruster._currentScale = thruster._scaleSpring.Update(thruster._currentScale, thrusterScale, Time.deltaTime);
            thruster.transform.localScale = Vector3.one * thruster._currentScale;
            thruster._light.range = thruster._baseLightRadius * thruster._currentScale;
            thruster._thrusterRenderer.enabled = thruster._currentScale > 0f;
            thruster._light.enabled = thruster._currentScale > 0f;
        }
    }
}
