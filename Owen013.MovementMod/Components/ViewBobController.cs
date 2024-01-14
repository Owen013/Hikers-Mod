using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class ViewBobController : MonoBehaviour
{
    public static ViewBobController s_instance;
    private PlayerCharacterController _characterController;
    private PlayerCameraController _cameraController;
    private PlayerAnimController _animController;
    private GameObject _viewBobRoot;
    private GameObject _toolBobRoot;
    private float _viewBobTimePosition;
    private float _viewBobIntensity;

    public void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(ViewBobController));
    }

    public void FixedUpdate()
    {
        if (_characterController == null) return;

        _viewBobTimePosition = Mathf.Repeat(_viewBobTimePosition + Time.fixedDeltaTime * _animController._animator.speed, 1);
        _viewBobIntensity = Mathf.Lerp(_viewBobIntensity, Mathf.Sqrt(Mathf.Pow(_animController._animator.GetFloat("RunSpeedX"), 2f) + Mathf.Pow(_animController._animator.GetFloat("RunSpeedY"), 2f)) * 0.02f, 0.25f);

        // camera bob
        float bobX = Mathf.Sin(2f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * ModController.s_instance.viewBobXSensitivity;
        float bobY = Mathf.Cos(4f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * ModController.s_instance.viewBobYSensitivity;
        _viewBobRoot.transform.localPosition = new Vector3(bobX, bobY, 0f);

        // tool bob
        float toolBobX = Mathf.Sin(2f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * ModController.s_instance.toolBobSensitivity * 0.5f;
        float toolBobY = Mathf.Cos(4f * Mathf.PI * _viewBobTimePosition) * _viewBobIntensity * ModController.s_instance.toolBobSensitivity * 0.25f;
        _toolBobRoot.transform.localPosition = new Vector3(toolBobX, toolBobY, 0f);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    public static void OnCharacterControllerStart()
    {
        s_instance._characterController = Locator.GetPlayerController();
        s_instance._cameraController = FindObjectOfType<PlayerCameraController>();
        s_instance._animController = FindObjectOfType<PlayerAnimController>();

        // create viewbob root and parent camera to it
        s_instance._viewBobRoot = new();
        s_instance._viewBobRoot.name = "ViewBobRoot";
        s_instance._viewBobRoot.transform.parent = s_instance._cameraController._playerCamera.mainCamera.transform.parent;
        s_instance._viewBobRoot.transform.localPosition = Vector3.zero;
        s_instance._viewBobRoot.transform.localRotation = Quaternion.identity;
        s_instance._cameraController._playerCamera.mainCamera.transform.parent = s_instance._viewBobRoot.transform;

        // create tool bob root and parent camera objects to it
        s_instance._toolBobRoot = new();
        s_instance._toolBobRoot.name = "ToolBobRoot";
        s_instance._toolBobRoot.transform.parent = s_instance._cameraController._playerCamera.mainCamera.transform;
        s_instance._toolBobRoot.transform.localPosition = Vector3.zero;
        s_instance._toolBobRoot.transform.localRotation = Quaternion.identity;
        s_instance._cameraController._playerCamera.mainCamera.transform.Find("ItemCarryTool").transform.parent = s_instance._toolBobRoot.transform;
        s_instance._cameraController._playerCamera.mainCamera.transform.Find("ProbeLauncher").transform.parent = s_instance._toolBobRoot.transform;
        s_instance._cameraController._playerCamera.mainCamera.transform.Find("FlashlightRoot").transform.parent = s_instance._toolBobRoot.transform;
        s_instance._cameraController._playerCamera.mainCamera.transform.Find("Signalscope").transform.parent = s_instance._toolBobRoot.transform;
        s_instance._cameraController._playerCamera.mainCamera.transform.Find("NomaiTranslatorProp").transform.parent = s_instance._toolBobRoot.transform;
    }
}