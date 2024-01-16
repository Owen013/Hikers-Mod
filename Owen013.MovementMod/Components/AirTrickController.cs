using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class AirTrickController : MonoBehaviour
{
    public static AirTrickController Instance;
    private PlayerCharacterController _characterController;
    private JetpackThrusterModel _jetpackModel;
    private GameObject _flipRoot;
    private Vector3 _flipVector;
    private bool _isFlipping;

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(typeof(AirTrickController));
    }

    private void Start()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        _jetpackModel = FindObjectOfType<JetpackThrusterModel>();

        CreateFlipRoot();
    }

    private void FixedUpdate()
    {
        if (_characterController == null) return;

        if (Main.Instance.isAirTricksEnabled && !_characterController.IsGrounded() && !PlayerState.InZeroG() && OWInput.IsPressed(InputLibrary.rollMode, InputMode.Character))
        {
            _isFlipping = true;
            _jetpackModel.DeactivateBoost();
            Vector2 mouseInput = OWInput.GetAxisValue(InputLibrary.look);
            _flipVector = Vector3.ClampMagnitude(_flipVector + new Vector3(-mouseInput.y * Time.fixedDeltaTime, 0f, -mouseInput.x * Time.fixedDeltaTime) * 360f * Main.Instance.airTrickSensitivity, Main.Instance.maxAirTrickMomentum);
            _flipRoot.transform.localRotation *= Quaternion.Euler(_flipVector * Time.fixedDeltaTime);
        }
        else
        {
            _isFlipping = false;
            _flipVector = Vector3.zero;
            _flipRoot.transform.localRotation = Quaternion.RotateTowards(_flipRoot.transform.localRotation, Quaternion.identity, Time.fixedDeltaTime * 180f);
        }
    }

    private void CreateFlipRoot()
    {
        Camera camera = Locator.GetPlayerCamera().mainCamera;
        GameObject playerModel = Instance._characterController.GetComponentInChildren<PlayerAnimController>().gameObject;

        // create flip root and parent camera and playermodel to it
        _flipRoot = new();
        _flipRoot.name = "FlipRoot";
        _flipRoot.transform.parent = camera.transform.parent;
        _flipRoot.transform.localPosition = Vector3.zero;
        _flipRoot.transform.localRotation = Quaternion.identity;
        camera.transform.parent = _flipRoot.transform;
        playerModel.transform.parent = _flipRoot.transform;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateTurning))]
    public static bool UpdatePlayerTurning()
    {
        if (Instance._isFlipping)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateInput))]
    public static bool UpdateCameraInput()
    {
        if (Instance._isFlipping)
        {
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    private static void OnGetJetpackInput(ref Vector3 __result)
    {
        if (Instance._isFlipping && __result.magnitude > 0f)
        {
            __result = Vector3.zero;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.IsBoosterAllowed))]
    public static void IsBoosterAllowed(ref bool __result)
    {
        if (Instance._isFlipping) __result = false;
    }
}