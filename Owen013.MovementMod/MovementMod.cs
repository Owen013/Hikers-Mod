using OWML.ModHelper;
using OWML.Common;
using UnityEngine.InputSystem;
using UnityEngine;
using Harmony;

namespace MovementMod
{
    public class MovementMod : ModBehaviour
    {
        private PlayerCharacterController playerController;
        private PlayerAnimController animController;

        public static bool enableTapJump;
        public static bool disableStrafeSlow;
        public static bool enableSprint;

        // To reference the mod from the patches we need a static reference to it
        public static MovementMod Instance;

        public bool IsDownThrustDisabled;

        private void Awake()
        {
            // This lets us use the mod in the patches
            Instance = this;
        }

        private void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"{nameof(MovementMod)} is loaded!", MessageType.Success);

            // We have to tell it to apply the patches we made below
            // Postfix means it calls the patch method after the original one is called
            ModHelper.HarmonyHelper.AddPostfix<JetpackThrusterController>
                ("GetRawInput",
                typeof(Patches),
                nameof(Patches.JetpackThrusterControllerGetRawInput));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>
                ("Awake",
                typeof(Patches),
                nameof(Patches.PlayerCharacterControllerAwake));
            ModHelper.Console.WriteLine($"Applied patches.");

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                Setup();
                ModHelper.Console.WriteLine($"MovementMod is loaded and ready to go!", MessageType.Success);
            };
        }

        // This runs whenever config.json is changed.
        public override void Configure(IModConfig config)
        {
            enableTapJump = config.GetSettingsValue<bool>("Tap to Jump Enabled");
            disableStrafeSlow = config.GetSettingsValue<bool>("Disable Strafe Slowdown");
            enableSprint = config.GetSettingsValue<bool>("Enable Sprinting");
            Setup();
        }

        // This is where MovementMod features are enabled or disabled.
        public void Setup()
        {
            if (LoadManager.s_currentScene != OWScene.SolarSystem
                && LoadManager.s_currentScene != OWScene.EyeOfTheUniverse
                && playerController == null)
                return;
            playerController = FindObjectOfType<PlayerCharacterController>();
            ModHelper.Events.Unity.FireInNUpdates(() => animController = FindObjectOfType<PlayerAnimController>(), 60);
            playerController._useChargeJump = !enableTapJump;
            playerController._strafeSpeed = 4f;
            if (disableStrafeSlow) playerController._strafeSpeed = 6f;
        }

        private void Update()
        {
            if (LoadManager.s_currentScene != OWScene.SolarSystem
                && LoadManager.s_currentScene != OWScene.EyeOfTheUniverse)
                return;
            {
                if (!OWInput.IsInputMode(InputMode.Menu))
                {
                    bool startSprint = false;
                    bool stopSprint = false;

                    // We use the same controls as the downward thrust input for the jetpack
                    startSprint = OWInput.IsNewlyPressed(InputLibrary.thrustDown);
                    stopSprint = OWInput.IsNewlyReleased(InputLibrary.thrustDown);

                    // To sprint we have to be standing on the ground
                    if (startSprint
                        && Locator.GetPlayerController().IsGrounded())
                        StartSprinting();
                    if (stopSprint)
                        StopSprinting();
                }
            }
        }

        // I moved these to their own methods because there are two places were we might start sprinting from
        public void StartSprinting()
        {
            if (enableSprint != true) return;
            IsDownThrustDisabled = true;
            playerController._runSpeed = 9f;
            playerController._strafeSpeed = 4f;
            if (disableStrafeSlow) playerController._strafeSpeed = 9f;
            animController._animator.speed = 1.5f;
        }

        public void StopSprinting()
        {
            if (enableSprint != true) return;
            IsDownThrustDisabled = false;
            playerController._runSpeed = 6f;
            playerController._strafeSpeed = 4f;
            if (disableStrafeSlow) playerController._strafeSpeed = 6f;
            animController._animator.speed = 1f;
        }
    }


    public static class Patches
    {
        // To disable the jetpack downward thrust while on the ground we have to patch the GetRawInput method in the JetpackThrusterController class
        public static void JetpackThrusterControllerGetRawInput(ref Vector3 __result)
        {
            // When grounded, don't let the input have any downward component in the y-direction
            if(MovementMod.Instance.IsDownThrustDisabled)
            {
                if (__result.y < 0) __result.y = 0;
            }
        }

        // When the player character controller is loaded we add to one of its events
        public static void PlayerCharacterControllerAwake(PlayerCharacterController __instance)
        {
            // If we are flying and just became grounded and control is still held then we have to start running
            __instance.OnBecomeGrounded += () =>
            {
                if (OWInput.IsPressed(InputLibrary.thrustDown))
                {
                    // This is where the Instance variable we made comes in use
                    MovementMod.Instance.StartSprinting();
                }
            };
        }
    }
}
