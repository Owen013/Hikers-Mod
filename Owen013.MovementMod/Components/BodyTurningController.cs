using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class BodyTurningController : MonoBehaviour
{
    public static BodyTurningController s_instance;
    public OWRigidbody _playerBody;
    public PlayerAnimController _animController;
    public PlayerCameraController _cameraController;
    public GameObject _turningRoot;
    public float _lastBodyAngle;

    private void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(BodyTurningController));
    }

    private void FixedUpdate()
    {
        if (_playerBody == null) return;

        _turningRoot.transform.localRotation *= Quaternion.Euler(0f, _playerBody.transform.rotation.eulerAngles.y - _lastBodyAngle, 0f);
        _lastBodyAngle = _playerBody.transform.rotation.eulerAngles.y;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    public static void OnCharacterStart()
    {
        s_instance._playerBody = Locator.GetPlayerBody();
        s_instance._animController = FindObjectOfType<PlayerAnimController>();
        s_instance._cameraController = FindObjectOfType<PlayerCameraController>();

        s_instance._turningRoot = new();
        s_instance._turningRoot.name = "TurningRoot";
        s_instance._turningRoot.transform.parent = s_instance._animController.transform.parent;
        s_instance._turningRoot.transform.localPosition = Vector3.zero;
        s_instance._turningRoot.transform.localRotation = Quaternion.identity;
        s_instance._animController.transform.parent = s_instance._turningRoot.transform;
    }
}