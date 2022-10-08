using HarmonyLib;
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
            if (HikersMod.disableDownThrust && __result.y < 0) __result.y = 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DreamLanternItem), nameof(DreamLanternItem.UpdateFocus))]
        public static void DreamLanternFocusChanged(DreamLanternItem __instance)
        {
            if (__instance._wasFocusing == __instance._focusing) return;
            HikersMod.dreamLanternFocused = __instance._focusing;
            HikersMod.dreamLanternFocusChanged = true;
            HikersMod.Instance.PrintLog("Dream lantern focus changed");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateAirControl))]
        public static bool CharacterUpdateAirControl()
        {
            if (!HikersMod.Instance.moreAirControlEnabled) return true;
            PlayerCharacterController characterController = HikersMod.Instance.characterController;
            if (characterController == null) return true;
            if (characterController._lastGroundBody != null)
            {
                Vector3 b = characterController._transform.InverseTransformDirection(characterController._lastGroundBody.GetPointVelocity(characterController._transform.position));
                Vector3 vector = characterController._transform.InverseTransformDirection(characterController._owRigidbody.GetVelocity()) - b;
                vector.y = 0f;
                float num = Time.fixedDeltaTime * 60f;
                float num2 = characterController._airAcceleration * num;
                Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);
                Vector3 localVelocityChange = new Vector3(num2 * axisValue.x, 0f, num2 * axisValue.y);
                if (vector.magnitude < characterController._airSpeed || (vector + localVelocityChange).magnitude < vector.magnitude)
                {
                    characterController._owRigidbody.AddLocalVelocityChange(localVelocityChange);
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
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.OnExitDreamWorld))]
        public static void ExitedDreamWorld()
        {
            HikersMod.Instance.isDreaming = false;
            HikersMod.Instance.UpdateMoveSpeed();
        }
    }
}
