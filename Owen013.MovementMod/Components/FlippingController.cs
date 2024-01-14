using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

public class FlippingController : MonoBehaviour
{
    public static FlippingController s_instance;
    private PlayerCharacterController _characterController;
    private bool _isFlipping;

    public void Awake()
    {
        s_instance = this;
        Harmony.CreateAndPatchAll(typeof(FlippingController));
    }

    public void FixedUpdate()
    {
        if (_characterController == null) return;

        if (!_characterController.IsGrounded()/* && !PlayerState.InZeroG()*/ && OWInput.IsPressed(InputLibrary.rollMode/*, InputMode.Character*/))
        {
            _isFlipping = true;
            _characterController._owRigidbody.UnfreezeRotation();
            Vector2 mouseInput = OWInput.GetAxisValue(InputLibrary.look);
            _characterController._owRigidbody.AddLocalAngularVelocityChange(new Vector3(-mouseInput.y * Time.fixedDeltaTime * 5f, 0f, -mouseInput.x * 5f * Time.fixedDeltaTime));
        }
        else
        {
            _isFlipping = false;
            if (!PlayerState.IsDead())
            {
                _characterController._owRigidbody.FreezeRotation();
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    public static void OnCharacterControllerStart()
    {
        s_instance._characterController = Locator.GetPlayerController();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateTurning))]
    public static bool UpdatePlayerTurning()
    {
        if (s_instance._isFlipping)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateInput))]
    public static bool UpdateCameraInput()
    {
        if (s_instance._isFlipping)
        {
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.IsBoosterAllowed))]
    public static void IsBoosterAllowed(ref bool __result)
    {
        if (s_instance._isFlipping) __result = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.OnBreakAlignment))]
    public static bool OnBreakAlignment()
    {
        if (s_instance._isFlipping) return false;
        else return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AlignPlayerWithForce), nameof(AlignPlayerWithForce.ManagedFixedUpdate))]
    public static void OnBreakAlignment(AlignPlayerWithForce __instance)
    {
        if (s_instance._isFlipping) __instance.SkipNextFrame();
    }
}