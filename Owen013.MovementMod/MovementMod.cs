using OWML.ModHelper;
using OWML.Common;
using UnityEngine;

namespace MovementMod
{
    public class MovementMod : ModBehaviour
    {
        // Setup() vars
        private PlayerCharacterController playerController;
        private PlayerAnimController animController;
        // Config options
        public static bool enableTapJump;
        public static bool disableStrafeSlow;
        public static bool enableSprint;
        // Sprinting patch vars
        public static MovementMod Instance;
        public bool IsDownThrustDisabled;

        private void Awake()
        {
            // Static reference to MovementMod so that it can be used in patches
            Instance = this;
        }

        private void Start()
        {
            ModHelper.Console.WriteLine($"{nameof(MovementMod)} is ready to go!", MessageType.Success);

            // xen magic stuff
            // We have to tell it to apply the patches we made below - xen-42
            // Postfix means it calls the patch method after the original one is called - xen
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
            };
        }

        // This runs whenever config.json is changed.
        public override void Configure(IModConfig config)
        {
            base.Configure(config); // Don't know what this does, but Configure() automatically creates it.
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
                    bool startSprint;
                    bool stopSprint;
                    // We use the same controls as the downward thrust input for the jetpack - xen
                    startSprint = OWInput.IsNewlyPressed(InputLibrary.thrustDown);
                    stopSprint = OWInput.IsNewlyReleased(InputLibrary.thrustDown);
                    // To sprint we have to be standing on the ground - xen
                    if (startSprint
                        && Locator.GetPlayerController().IsGrounded())
                        StartSprinting();
                    if (stopSprint)
                        StopSprinting();
                }
            }
        }

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
        // To disable the jetpack downward thrust while on the ground we have to patch the GetRawInput method in the JetpackThrusterController class - xen
        public static void JetpackThrusterControllerGetRawInput(ref Vector3 __result)
        {
            // When grounded, don't let the input have any downward component in the y-direction - xen
            if (MovementMod.Instance.IsDownThrustDisabled)
            {
                if (__result.y < 0) __result.y = 0;
            }
        }

        // When the player character controller is loaded we add to one of its events - xen
        public static void PlayerCharacterControllerAwake(PlayerCharacterController __instance)
        {
            // If we are flying and just became grounded and control is still held then we have to start running - xen
            __instance.OnBecomeGrounded += () =>
            {
                if (OWInput.IsPressed(InputLibrary.thrustDown))
                {
                    // This is where the Instance variable we made comes in use - xen
                    MovementMod.Instance.StartSprinting();
                }
            };
        }
    }
}
