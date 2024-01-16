using UnityEngine;

namespace HikersMod.Components;

public class ViewBobController : MonoBehaviour
{
    public static ViewBobController Instance;
    private PlayerCharacterController _characterController;
    private PlayerCameraController _cameraController;
    private PlayerAnimController _animController;
    private GameObject _viewBobRoot;
    private GameObject _toolBobRoot;
    private GameObject _scoutBobRoot;
    private float _viewBobTimePosition;
    private float _viewBobIntensity;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _cameraController = Locator.GetPlayerCameraController();
        _animController = GetComponentInChildren<PlayerAnimController>();

        // create viewbob root and parent camera to it
        _viewBobRoot = new();
        _viewBobRoot.name = "ViewBobRoot";
        _viewBobRoot.transform.parent = _cameraController._playerCamera.mainCamera.transform.parent;
        _viewBobRoot.transform.localPosition = Vector3.zero;
        _viewBobRoot.transform.localRotation = Quaternion.identity;
        _cameraController._playerCamera.mainCamera.transform.parent = _viewBobRoot.transform;

        // create tool bob root and parent camera objects to it
        _toolBobRoot = new();
        _toolBobRoot.name = "ToolBobRoot";
        _toolBobRoot.transform.parent = _cameraController._playerCamera.mainCamera.transform;
        _toolBobRoot.transform.localPosition = Vector3.zero;
        _toolBobRoot.transform.localRotation = Quaternion.identity;
        _cameraController._playerCamera.mainCamera.transform.Find("ItemCarryTool").transform.parent = _toolBobRoot.transform;
        _cameraController._playerCamera.mainCamera.transform.Find("FlashlightRoot").transform.parent = _toolBobRoot.transform;
        _cameraController._playerCamera.mainCamera.transform.Find("Signalscope").transform.parent = _toolBobRoot.transform;
        _cameraController._playerCamera.mainCamera.transform.Find("NomaiTranslatorProp").transform.parent = _toolBobRoot.transform;

        // create tool bob root and parent camera objects to it
        _scoutBobRoot = new();
        _scoutBobRoot.name = "ScoutBobRoot";
        _scoutBobRoot.transform.parent = _cameraController._playerCamera.mainCamera.transform;
        _scoutBobRoot.transform.localPosition = Vector3.zero;
        _scoutBobRoot.transform.localRotation = Quaternion.identity;
        _cameraController._playerCamera.mainCamera.transform.Find("ProbeLauncher").transform.parent = _scoutBobRoot.transform;
    }

    private void FixedUpdate()
    {
        if (_characterController == null) return;

        _viewBobTimePosition = Mathf.Repeat(_viewBobTimePosition + Time.fixedDeltaTime * 1.03f * _animController._animator.speed, 1);
        _viewBobIntensity = Mathf.Lerp(_viewBobIntensity, Mathf.Sqrt(Mathf.Pow(_animController._animator.GetFloat("RunSpeedX"), 2f) + Mathf.Pow(_animController._animator.GetFloat("RunSpeedY"), 2f)) * 0.02f, 0.25f);

        // camera bob
        float bobX = Mathf.Sin(2f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * Main.Instance.viewBobXSensitivity;
        float bobY = Mathf.Cos(4f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * Main.Instance.viewBobYSensitivity;
        _viewBobRoot.transform.localPosition = new Vector3(bobX, bobY, 0f);

        // tool bob
        float toolBobX = Mathf.Sin(2f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * Main.Instance.toolBobSensitivity * 0.5f;
        toolBobX *= Main.Instance.SmolHatchlingAPI != null ? Main.Instance.SmolHatchlingAPI.GetCurrentScale().x : 1f;
        float toolBobY = Mathf.Cos(4f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * Main.Instance.toolBobSensitivity * 0.25f;
        toolBobY *= Main.Instance.SmolHatchlingAPI != null ? Main.Instance.SmolHatchlingAPI.GetCurrentScale().y : 1f;
        _toolBobRoot.transform.localPosition = new Vector3(toolBobX, toolBobY, 0f);

        // separate root for scout launcher since it's less reactive ingame
        _scoutBobRoot.transform.localPosition = _toolBobRoot.transform.localPosition * 3f;
    }
}