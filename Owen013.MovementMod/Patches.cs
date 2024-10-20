using GhostEnums;
using HarmonyLib;
using HikersMod.Components;
using UnityEngine;

namespace HikersMod;

[HarmonyPatch]
public static class Patches
{
    // Add components to character
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
    private static void OnCharacterControllerStart(PlayerCharacterController __instance)
    {
        __instance.gameObject.AddComponent<CharacterAttributeController>();
        __instance.gameObject.AddComponent<SprintingController>();
        __instance.gameObject.AddComponent<EmergencyBoostController>();
        __instance.gameObject.AddComponent<FloatyPhysicsController>();
        __instance.gameObject.AddComponent<WallJumpController>();
        __instance.gameObject.AddComponent<JetpackSprintEffectController>();
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

    // allows turning in midair
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
    private static bool UpdateAirControl(PlayerCharacterController __instance)
    {
        // if feature is disabled then just do the vanilla method
        if (!ModMain.IsMidairTurningEnabled) return true;

        if (__instance._lastGroundBody != null)
        {
            // get player's horizontal velocity
            Vector3 pointVelocity = __instance._transform.InverseTransformDirection(__instance._lastGroundBody.GetPointVelocity(__instance._transform.position));
            Vector3 localVelocity = __instance._transform.InverseTransformDirection(__instance._owRigidbody.GetVelocity()) - pointVelocity;
            localVelocity.y = 0f;

            float physicsTime = Time.fixedDeltaTime * 60f;
            float acceleration = __instance._airAcceleration * physicsTime;
            Vector2 moveInput = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);
            Vector3 localVelocityChange = new(acceleration * moveInput.x, 0f, acceleration * moveInput.y);

            // new velocity can't be more than old velocity and airspeed
            float maxSpeed = Mathf.Max(localVelocity.magnitude, __instance._airSpeed);
            Vector3 newLocalVelocity = Vector3.ClampMagnitude(localVelocity + localVelocityChange, maxSpeed);

            // cancel out old velocity, add new one
            __instance._owRigidbody.AddLocalVelocityChange(newLocalVelocity - localVelocity);
        }
        return false;
    }

    // add component to animator(s)
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAnimController), nameof(PlayerAnimController.Start))]
    private static void OnAnimControllerStart(PlayerAnimController __instance)
    {
        __instance.gameObject.AddComponent<AnimSpeedController>();
    }

    // adjusts footstep sound based on player speed
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerMovementAudio), nameof(PlayerMovementAudio.PlayFootstep))]
    private static bool PlayFootstep(PlayerMovementAudio __instance)
    {
        AudioType audioType = (!PlayerState.IsCameraUnderwater() && __instance._fluidDetector.InFluidType(FluidVolume.Type.WATER)) ? AudioType.MovementShallowWaterFootstep : PlayerMovementAudio.GetFootstepAudioType(__instance._playerController.GetGroundSurface());
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

    // changes dream lantern max speed to config setting
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.OverrideMaxRunSpeed))]
    private static bool OverrideMaxRunSpeed(ref float maxSpeedX, ref float maxSpeedZ, DreamLanternItem __instance)
    {
        float lerpPosition = 1f - __instance._lanternController.GetFocus();
        lerpPosition *= lerpPosition;
        maxSpeedX = Mathf.Lerp(ModMain.DreamLanternSpeed, maxSpeedX, lerpPosition);
        maxSpeedZ = Mathf.Lerp(ModMain.DreamLanternSpeed, maxSpeedZ, lerpPosition);
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
        if (SprintingController.Instance.IsSprinting == true)
        {
            __result = false;
        }
    }

    // makes ghosts run faster when player is sprinting
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetMoveSpeed))]
    private static void GhostGetMoveSpeed(MoveType moveType, ref float __result)
    {
        if (moveType == MoveType.CHASE && SprintingController.Instance.IsSprinting == true)
        {
            __result *= ModMain.SprintMultiplier;
        }
    }

    // makes ghosts speed up faster when player is sprinting
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GhostConstants), nameof(GhostConstants.GetMoveAcceleration))]
    private static void GhostGetMoveAcceleration(MoveType moveType, ref float __result)
    {
        if (moveType == MoveType.CHASE && SprintingController.Instance.IsSprinting == true)
        {
            __result *= ModMain.SprintMultiplier;
        }
    }
}