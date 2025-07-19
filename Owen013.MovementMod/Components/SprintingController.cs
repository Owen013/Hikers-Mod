using HarmonyLib;
using UnityEngine;

namespace HikersMod.Components;

[HarmonyPatch]
public class SprintingController : MonoBehaviour
{
    public static SprintingController Instance { get; private set; }

    public bool IsSprinting { get; private set; }

    private PlayerCharacterController _characterController;

    private IInputCommands _sprintButton;

    private void StartSprinting()
    {
        IsSprinting = true;
        _characterController._runSpeed = ModMain.Instance.RunSpeed * ModMain.Instance.SprintMultiplier;
        _characterController._strafeSpeed = ModMain.Instance.StrafeSpeed * ModMain.Instance.SprintMultiplier;
    }

    private void StopSprinting()
    {
        IsSprinting = false;
        _characterController._runSpeed = ModMain.Instance.RunSpeed;
        _characterController._strafeSpeed = ModMain.Instance.StrafeSpeed;
    }

    private void UpdateSprintingState()
    {
        bool isOnValidGround = _characterController.IsGrounded() && !_characterController.IsSlidingOnIce();
        if (base.enabled && isOnValidGround && OWInput.IsPressed(_sprintButton) && (IsSprinting || OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0f))
        {
            StartSprinting();
        }
        else
        {
            StopSprinting();
        }
    }

    private void OnConfigure()
    {
        base.enabled = ModMain.Instance.IsSprintingEnabled;

        // Change built-in character attributes
        _sprintButton = ModMain.Instance.SprintButton == "Up Thrust" ? InputLibrary.thrustUp : InputLibrary.thrustDown;

        UpdateSprintingState();
    }

    private void Awake()
    {
        Instance = this;
        _characterController = GetComponent<PlayerCharacterController>();

        _characterController.OnBecomeGrounded += () =>
        {
            if (ModMain.Instance.ShouldSprintOnLanding)
            {
                UpdateSprintingState();
            }
        };

        ModMain.Instance.OnConfigure += OnConfigure;
        OnConfigure();
    }

    private void Update()
    {
        // check both thrust buttons so that the player can thrust out of a sprint
        bool hasVerticalThrustChanged = OWInput.IsNewlyPressed(_sprintButton) || OWInput.IsNewlyReleased(_sprintButton) || OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.thrustDown);
        if (hasVerticalThrustChanged || (OWInput.IsNewlyPressed(InputLibrary.boost) && !_characterController.IsGrounded()))
        {
            UpdateSprintingState();
        }
    }

    private void OnDisable()
    {
        StopSprinting();
    }

    private void OnDestroy()
    {
        ModMain.Instance.OnConfigure -= OnConfigure;
    }

    // allows the player to jump while sprinting
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
    private static bool CharacterControllerUpdate(PlayerCharacterController __instance)
    {
        if (!__instance._isAlignedToForce && !__instance._isZeroGMovementEnabled) return false;

        // normal Update() function, but added isWearingSuit and IsSprintModeActive to if statement. The rest of this method is unmodified.
        if (!__instance._isWearingSuit || SprintingController.Instance.IsSprinting == true || OWInput.GetValue(InputLibrary.thrustUp, InputMode.All) == 0f)
        {
            __instance.UpdateJumpInput();
        }
        else
        {
            __instance._jumpChargeTime = 0f;
            __instance._jumpNextFixedUpdate = false;
            __instance._jumpPressedInOtherMode = false;
        }

        if (__instance._isZeroGMovementEnabled)
        {
            __instance._pushPrompt.SetVisibility(OWInput.IsInputMode(InputMode.Character | InputMode.NomaiRemoteCam) && __instance._isPushable);
        }

        return false;
    }

    // prevents player from using jetpack while they are sprinting
    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    private static void OnGetJetpackInput(ref Vector3 __result)
    {
        if (__result.y != 0f && SprintingController.Instance.IsSprinting == true)
        {
            __result.y = 0f;
        }
    }

    // prevents player from using booster on the very first frame after jumping out of a sprint
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.IsBoosterAllowed))]
    private static void OnCheckIsBoosterAllowed(ref bool __result)
    {
        if (Instance.IsSprinting == true)
        {
            __result = false;
        }
    }

    // makes ghosts run faster when player is sprinting
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetMoveSpeed))]
    private static void GhostGetMoveSpeed(GhostEnums.MoveType moveType, ref float __result)
    {
        if (moveType == GhostEnums.MoveType.CHASE && Instance.IsSprinting == true)
        {
            __result *= ModMain.Instance.SprintMultiplier;
        }
    }

    // makes ghosts speed up faster when player is sprinting
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetMoveAcceleration))]
    private static void GhostGetMoveAcceleration(GhostEnums.MoveType moveType, ref float __result)
    {
        if (moveType == GhostEnums.MoveType.CHASE && Instance.IsSprinting == true)
        {
            __result *= ModMain.Instance.SprintMultiplier;
        }
    }
}