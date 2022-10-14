﻿using HarmonyLib;
using OWML.Common;
using UnityEngine;

namespace HikersMod
{
    public static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Start))]
        public static void CharacterControllerStart()
        {
            HikersMod.Instance.OnCharacterStart();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
        public static void GetJetpackInput(ref Vector3 __result)
        {
            if (HikersMod.Instance.disableDownThrust && __result.y < 0) __result.y = 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.UpdateFocus))]
        public static void DreamLanternFocusChanged(DreamLanternItem __instance)
        {
            if (__instance._wasFocusing == __instance._focusing) return;
            HikersMod.Instance.dreamLanternFocused = __instance._focusing;
            HikersMod.Instance.dreamLanternFocusChanged = true;
            if (__instance._focusing) HikersMod.Instance.PrintLog("Focused Dream Lantern", MessageType.Info);
            else HikersMod.Instance.PrintLog("Unfocused Dream Lantern", MessageType.Info);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
        public static bool CharacterUpdateAirControl(PlayerCharacterController __instance)
        {
            if (!HikersMod.Instance.moreAirControlEnabled) return true;
            if (__instance == null) return true;
            if (__instance._lastGroundBody != null)
            {
                Vector3 b = __instance._transform.InverseTransformDirection(__instance._lastGroundBody.GetPointVelocity(__instance._transform.position));
                Vector3 vector = __instance._transform.InverseTransformDirection(__instance._owRigidbody.GetVelocity()) - b;
                vector.y = 0f;
                float num = Time.fixedDeltaTime * 60f;
                float num2 = __instance._airAcceleration * num;
                Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);
                Vector3 localVelocityChange = new Vector3(num2 * axisValue.x, 0f, num2 * axisValue.y);
                if (vector.magnitude < __instance._airSpeed || (vector + localVelocityChange).magnitude <= vector.magnitude)
                {
                    __instance._owRigidbody.AddLocalVelocityChange(localVelocityChange);
                }
            }
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnEnterDreamWorld))]
        public static void EnteredDreamWorld()
        {
            HikersMod.Instance.isDreaming = true;
            HikersMod.Instance.UpdateMoveSpeed();
            HikersMod.Instance.PrintLog("Entered Dream World", MessageType.Info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnExitDreamWorld))]
        public static void ExitedDreamWorld()
        {
            HikersMod.Instance.isDreaming = false;
            HikersMod.Instance.UpdateMoveSpeed();
            HikersMod.Instance.PrintLog("Left Dream World", MessageType.Info);
        }
    }
}