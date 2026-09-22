using HarmonyLib;
using HikersMod.Components;
using OWML.Common;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace HikersMod;

[HarmonyPatch]
public static class Patches
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
    public static IEnumerable<CodeInstruction> DreamLanternItem_OverrideMaxRunSpeed_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var patchedInstructions = new List<CodeInstruction>(instructions);

        // Find insertion index of patch instructions.
        int insertionIndex = -1;
        for (int i = 0; i < patchedInstructions.Count; i++)
        {
            if (patchedInstructions[i].opcode == OpCodes.Ldc_R4 && patchedInstructions[i].operand is float number && number == 2f)
            {
                insertionIndex = i + 1;
                break;
            }
        }

        if (insertionIndex == -1)
        {
            ModMain.ModConsole.WriteLine($"Failed to find insertion index for DreamLanternItem.OverrideMaxRunSpeed transpiler.", MessageType.Error);
            return instructions;
        }

        patchedInstructions.InsertRange(insertionIndex,
        [
            // Multiplies value on stack by 0.5f, then multiplies value on stack by HikersMod.Config.DreamLanternSpeed.
            new(OpCodes.Ldc_R4, 0.5f),
            new(OpCodes.Mul),
            new(OpCodes.Call, AccessTools.PropertyGetter(typeof(Config), nameof(Config.DreamLanternSpeed))),
            new(OpCodes.Mul)
        ]);

        return patchedInstructions;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetMoveSpeed))]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetMoveAcceleration))]
    private static void GhostConstants_GetMoveSpeed_GetMoveAcceleration_Postfix(GhostEnums.MoveType moveType, ref float __result)
    {
        if (Config.SpeedUpGhostsWhileSprinting && SprintingController.Instance.IsSprinting && moveType == GhostEnums.MoveType.CHASE)
        {
            __result *= Config.SprintMultiplier;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetTurnSpeed))]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetTurnAcceleration))]
    private static void GhostConstants_GetTurnSpeed_GetTurnAcceleration_Postfix(GhostEnums.TurnSpeed turnSpeed, ref float __result)
    {
        if (Config.SpeedUpGhostsWhileSprinting && SprintingController.Instance.IsSprinting && turnSpeed == GhostEnums.TurnSpeed.FAST)
        {
            __result *= Config.SprintMultiplier;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    private static void OnGetJetpackInput(ref Vector3 __result)
    {
        // Prevent player from using vertical jetpack input while they are sprinting, as these actions both use the same key.
        if (__result.y != 0f && SprintingController.Instance.IsSprinting == true)
        {
            __result.y = 0f;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAnimController), nameof(PlayerAnimController.Start))]
    private static void PlayerAnimController_Start_Postfix(PlayerAnimController __instance)
    {
        __instance.gameObject.AddComponent<AnimSpeedController>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void PlayerCharacterController_Start_Postfix(PlayerCharacterController __instance)
    {
        __instance.gameObject.AddComponent<PlayerSettingsController>();
        __instance.gameObject.AddComponent<SprintingController>();
        __instance.gameObject.AddComponent<EmergencyBoostController>();
        __instance.gameObject.AddComponent<FloatyPhysicsController>();
        __instance.gameObject.AddComponent<WallJumpController>();
        __instance.gameObject.AddComponent<JetpackSprintEffectController>();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Update))]
    private static bool PlayerCharacterController_Update_Prefix(PlayerCharacterController __instance)
    {
        if (!__instance._isAlignedToForce && !__instance._isZeroGMovementEnabled) return false;

        // Vanilla Update() function, but added isWearingSuit and IsSprinting to if statement. The rest of this method is unmodified.
        if (!__instance._isWearingSuit || SprintingController.Instance.IsSprinting || OWInput.GetValue(InputLibrary.thrustUp, InputMode.All) == 0f)
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
    private static bool PlayerCharacterController_UpdateAirControl_Prefix(PlayerCharacterController __instance)
    {
        if (!Config.IsMidairTurningEnabled)
        {
            return true;
        }

        if (__instance._lastGroundBody != null)
        {
            Vector3 pointVelocity = __instance._transform.InverseTransformDirection(__instance._lastGroundBody.GetPointVelocity(__instance._transform.position));
            Vector3 localVelocity = __instance._transform.InverseTransformDirection(__instance._owRigidbody.GetVelocity()) - pointVelocity;
            localVelocity.y = 0f;

            float physicsTime = Time.fixedDeltaTime * 60f;
            float acceleration = __instance._airAcceleration * physicsTime;
            Vector2 moveInput = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);
            Vector3 localVelocityChange = new(acceleration * moveInput.x, 0f, acceleration * moveInput.y);

            float maxSpeed = Mathf.Max(localVelocity.magnitude, __instance._airSpeed);
            Vector3 newLocalVelocity = Vector3.ClampMagnitude(localVelocity + localVelocityChange, maxSpeed);

            __instance._owRigidbody.AddLocalVelocityChange(newLocalVelocity - localVelocity);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerMovementAudio), nameof(PlayerMovementAudio.PlayFootstep))]
    private static bool PlayerMovementAudio_PlayFootstep_Prefix(PlayerMovementAudio __instance)
    {
        bool isStandingInWater = !PlayerState.IsCameraUnderwater() && __instance._fluidDetector.InFluidType(FluidVolume.Type.WATER);
        AudioType audioType = isStandingInWater ? AudioType.MovementShallowWaterFootstep : PlayerMovementAudio.GetFootstepAudioType(__instance._playerController.GetGroundSurface());
        if (audioType != AudioType.None)
        {
            __instance._footstepAudio.pitch = Random.Range(0.9f, 1.1f);
            float audioVolume = 1.4f * Locator.GetPlayerController().GetRelativeGroundVelocity().magnitude / 6f;
            if (ModMain.SmolHatchlingAPI != null)
            {
                audioVolume /= ModMain.SmolHatchlingAPI.GetPlayerScale();
            }

            __instance._footstepAudio.PlayOneShot(audioType, audioVolume);
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.IsBoosterAllowed))]
    private static void PlayerResources_IsBoosterAllowed_Postfix(ref bool __result)
    {
        if (SprintingController.Instance.IsSprinting == true)
        {
            __result = false;
        }
    }
}