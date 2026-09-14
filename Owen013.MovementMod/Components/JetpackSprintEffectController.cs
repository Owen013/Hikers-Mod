using UnityEngine;

namespace HikersMod.Components;

public class JetpackSprintEffectController : MonoBehaviour
{
    private GameObject _playerSuitObject;

    private GameObject _playerJetpackObject;

    private JetpackThrusterAudio _jetpackThrusterAudio;

    private ThrusterFlameController[] _thrusterFlames;

    private Vector2 _thrusterFlameVector;

    private static void SetThrusterScale(ThrusterFlameController thruster, float thrusterScale)
    {
        if (thruster._underwater)
        {
            thrusterScale = 0f;
        }

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

    private void OnConfigure()
    {
        enabled = Config.IsSprintEffectEnabled;
    }

    private void Awake()
    {
        _jetpackThrusterAudio = GetComponentInChildren<JetpackThrusterAudio>();
        _thrusterFlames = GetComponentsInChildren<ThrusterFlameController>(true);
        _playerSuitObject = GetComponentInChildren<PlayerAnimController>().transform.Find("Traveller_Mesh_v01:Traveller_Geo").gameObject;
        _playerJetpackObject = _playerSuitObject.transform.Find("Traveller_Mesh_v01:Props_HEA_Jetpack").gameObject;
        _thrusterFlameVector = Vector2.zero;

        Config.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void LateUpdate()
    {
        bool isJetpackVisible = _playerJetpackObject.activeInHierarchy;
        bool isSprinting = SprintingController.Instance.IsSprinting;
        Vector2 targetFlameVector = isJetpackVisible && isSprinting ? OWInput.GetAxisValue(InputLibrary.moveXZ) : Vector2.zero;

        _thrusterFlameVector = Vector2.MoveTowards(_thrusterFlameVector, targetFlameVector, 5f * Time.deltaTime);
        Vector2 effectiveFlameVector = _thrusterFlameVector;
        effectiveFlameVector.x = Mathf.Clamp(effectiveFlameVector.x, -20f, 20f);
        effectiveFlameVector.y = Mathf.Clamp(effectiveFlameVector.y, -20f, 20f);
        if (!_jetpackThrusterAudio.isActiveAndEnabled)
        {
            float soundVolume = effectiveFlameVector.magnitude;
            float soundPan = -effectiveFlameVector.x * 0.4f;
            bool hasFuel = _jetpackThrusterAudio._playerResources.GetFuel() > 0f;
            bool isUnderwater = _jetpackThrusterAudio._underwater;
            _jetpackThrusterAudio.UpdateTranslationalSource(_jetpackThrusterAudio._translationalSource, soundVolume, soundPan, !isUnderwater && hasFuel);
            _jetpackThrusterAudio.UpdateTranslationalSource(_jetpackThrusterAudio._underwaterSource, soundVolume, soundPan, isUnderwater);
            _jetpackThrusterAudio.UpdateTranslationalSource(_jetpackThrusterAudio._oxygenSource, soundVolume, soundPan, !isUnderwater && !hasFuel);
        }

        for (int i = 0; i < _thrusterFlames.Length; i++)
        {
            if (_thrusterFlames[i].isActiveAndEnabled)
            {
                break;
            }

            switch (_thrusterFlames[i]._thruster)
            {
                case Thruster.Forward_LeftThruster:
                    SetThrusterScale(_thrusterFlames[i], effectiveFlameVector.y);
                    break;
                case Thruster.Forward_RightThruster:
                    SetThrusterScale(_thrusterFlames[i], effectiveFlameVector.y);
                    break;
                case Thruster.Left_Thruster:
                    SetThrusterScale(_thrusterFlames[i], -effectiveFlameVector.x);
                    break;
                case Thruster.Right_Thruster:
                    SetThrusterScale(_thrusterFlames[i], effectiveFlameVector.x);
                    break;
                case Thruster.Backward_LeftThruster:
                    SetThrusterScale(_thrusterFlames[i], -effectiveFlameVector.y);
                    break;
                case Thruster.Backward_RightThruster:
                    SetThrusterScale(_thrusterFlames[i], -effectiveFlameVector.y);
                    break;
            }
        }
    }

    private void OnDisable()
    {
        _thrusterFlameVector = Vector2.zero;
    }

    private void OnDestroy()
    {
        Config.OnConfigure -= OnConfigure;
    }
}