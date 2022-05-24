using System.Collections;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;

namespace MovementMod
{
    public interface ISmolHatchling
    {
        float GetAnimSpeed();
    }

    public class HikersMod : ModBehaviour
    {
        // Config vars
        bool chargeJumpDisabled, slowStrafeDisabled, sprintEnabled;
        public static float runSpeed, walkSpeed, jumpPower, sprintSpeed;

        // Mod vars
        PlayerCharacterController characterController;
        PlayerCameraController cameraController;
        public static PlayerAnimController animController;
        public static float runAnimSpeed, sprintAnimSpeed, walkAnimSpeed, strafeSpeed, sprintStrafeSpeed, climbsLeft;
        string moveState;
        bool allLoaded;

        // Patch vars
        public static HikersMod Instance;
        public static bool disableDownThrust, dreamLanternFocused;

        public override void Configure(IModConfig config)
        {
            base.Configure(config);
            chargeJumpDisabled = config.GetSettingsValue<bool>("Disable Charge-Jump");
            slowStrafeDisabled = config.GetSettingsValue<bool>("Disable Slow Strafing");
            runSpeed = config.GetSettingsValue<float>("Default Run Speed");
            walkSpeed = config.GetSettingsValue<float>("Walk Speed");
            jumpPower = config.GetSettingsValue<float>("Jump Power");
            sprintEnabled = config.GetSettingsValue<bool>("Enable Sprinting");
            sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
            Setup();
        }

        private void Awake()
        {
            // Static reference to HikersMod so it can be used in patches.
            Instance = this;
        }

        private void Start()
        {
            // Apply patches.
            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>(
                "Start",
                typeof(Patches),
                nameof(Patches.CharacterStart));

            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>(
                "Awake",
                typeof(Patches),
                nameof(Patches.CharacterAwake));

            ModHelper.HarmonyHelper.AddPostfix<JetpackThrusterController>(
                "GetRawInput",
                typeof(Patches),
                nameof(Patches.GetJetpackInput));

            ModHelper.HarmonyHelper.AddPostfix<DreamLanternItem>(
                "UpdateFocus",
                typeof(Patches),
                nameof(Patches.DreamLanternFocusChanged));

            // Ready!
            ModHelper.Console.WriteLine($"{nameof(HikersMod)} is ready to go!", MessageType.Success);
        }

        public void Setup()
        {
            float animSpeed;
            // Get Smol Hatchling
            if (ModHelper.Interaction.ModExists("Owen013.TeenyHatchling"))
            {
                ISmolHatchling smolHatchlingAPI = ModHelper.Interaction.GetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
                animSpeed = smolHatchlingAPI.GetAnimSpeed();
            }
            else animSpeed = 1;

            // Make sure that the scene is the SS or Eye
            if (WrongScene()) return;

            // Get vars
            characterController = Locator.GetPlayerController();
            cameraController = Locator.GetPlayerCameraController();
            animController = FindObjectOfType<PlayerAnimController>();

            characterController._useChargeJump = !chargeJumpDisabled;
            characterController._runSpeed = runSpeed;
            characterController._strafeSpeed = strafeSpeed;
            characterController._walkSpeed = walkSpeed;
            characterController._maxJumpSpeed = jumpPower;

            if (slowStrafeDisabled)
            {
                strafeSpeed = runSpeed;
                sprintStrafeSpeed = sprintSpeed;
            }
            else
            {
                strafeSpeed = (2f / 3f) * runSpeed;
                sprintStrafeSpeed = (2f / 3f) * sprintSpeed;
            }

            runAnimSpeed = Mathf.Max(runSpeed / 6 * animSpeed, animSpeed);
            sprintAnimSpeed = Mathf.Max(sprintSpeed / 6 * animSpeed, animSpeed);
            walkAnimSpeed = Mathf.Max(walkSpeed / 6 * animSpeed, animSpeed);

            // The Update() code won't run until after Setup() has at least once
            allLoaded = true;
        }

        private void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (WrongScene() || !allLoaded) return;

            // Sprinting
            bool grounded = characterController._isGrounded;
            bool holdingLantern = characterController._heldLanternItem != null;
            bool walking = (OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern) || dreamLanternFocused;
            bool canSprint = sprintEnabled && !walking;
            bool sprintKeyHeld = OWInput.IsPressed(InputLibrary.thrustDown);

            if (moveState != "Sprinting" && canSprint && grounded && sprintKeyHeld) ChangeState("Sprinting");
            else if (moveState != "Walking" && walking) ChangeState("Walking");
            else if (moveState != "Normal" && !sprintKeyHeld && !walking) ChangeState("Normal");
        }

        public void ChangeState(string state)
        {
            moveState = state;
            switch (state)
            {
                case "Normal":
                    ModHelper.Console.WriteLine($"State: Normal");
                    characterController._runSpeed = runSpeed;
                    characterController._strafeSpeed = strafeSpeed;
                    animController._animator.speed = runAnimSpeed;
                    disableDownThrust = false;
                    break;
                case "Walking":
                    ModHelper.Console.WriteLine($"State: Walking");
                    animController._animator.speed = walkAnimSpeed;
                    disableDownThrust = false;
                    break;
                case "Sprinting":
                    ModHelper.Console.WriteLine($"State: Sprinting");
                    characterController._runSpeed = sprintSpeed;
                    characterController._strafeSpeed = sprintStrafeSpeed;
                    animController._animator.speed = sprintAnimSpeed;
                    disableDownThrust = true;
                    break;
            }
        }

        private bool WrongScene()
        {
            OWScene scene = LoadManager.s_currentScene;
            return !(scene == OWScene.SolarSystem || scene == OWScene.EyeOfTheUniverse);
        }
    }

    public static class Patches
    {
        public static void CharacterStart(PlayerCharacterController __instance)
        {
            HikersMod.Instance.Setup();
        }

        public static void CharacterAwake(PlayerCharacterController __instance)
        {
            __instance.OnBecomeGrounded += () =>
            {
                if (OWInput.IsPressed(InputLibrary.thrustDown) && !OWInput.IsPressed(InputLibrary.rollMode))
                    HikersMod.Instance.ChangeState("Sprinting");
            };
        }

        public static void GetJetpackInput(ref Vector3 __result)
        {
            if (HikersMod.disableDownThrust && __result.y < 0) __result.y = 0;
        }

        public static void DreamLanternFocusChanged(DreamLanternItem __instance)
        {
            HikersMod.dreamLanternFocused = __instance._focusing;
        }
    }
}