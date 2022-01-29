using OWML.ModHelper;
using OWML.Common;
using UnityEngine;

namespace MovementMod
{
    public class HikersMod : ModBehaviour
    {
        // Setup() vars
        private PlayerCharacterController playerController;
        private PlayerAnimController animController;
        public DreamLanternController dreamLantern;

        // Config options
        public static bool disableChargeJump;
        public static bool disableSlowerStrafing;
        public static bool enableSprint;
        public static float defaultSpeed;
        public static float sprintSpeed;
        public static float walkSpeed;
        public static float jumpPower;

        // Sprinting patch vars
        public static HikersMod Instance;
        public bool IsDownThrustDisabled;
        public bool IsFocusingLantern;
        public bool StoppedFocusingLantern;

        // Other vars
        public static float strafeSpeed;

        private void Awake()
        {
            // Static reference to MovementMod so that it can be used in patches
            Instance = this;
        }

        private void Start()
        {
            ModHelper.Console.WriteLine($"{nameof(HikersMod)} is ready to go!", MessageType.Success);

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
            ModHelper.HarmonyHelper.AddPostfix<DreamLanternItem>
                ("UpdateFocus",
                typeof(Patches),
                nameof(Patches.DreamLanternFocusChanged));
            ModHelper.Console.WriteLine($"Applied patches.");

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                Setup();
            };
        }

        // This runs whenever config.json is changed.
        public override void Configure(IModConfig config)
        {
            // Update config vars
            base.Configure(config); // Don't know what this does, but Configure() automatically creates it.
            disableChargeJump = config.GetSettingsValue<bool>("Disable Charge-Jump");
            disableSlowerStrafing = config.GetSettingsValue<bool>("Disable Slower Strafing");
            enableSprint = config.GetSettingsValue<bool>("Enable Sprinting");
            defaultSpeed = config.GetSettingsValue<float>("Default Speed");
            sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
            walkSpeed = config.GetSettingsValue<float>("Walk Speed");
            jumpPower = config.GetSettingsValue<float>("Jump Power");

            // Update strafe speed
            if (disableSlowerStrafing) strafeSpeed = defaultSpeed;
            else strafeSpeed = defaultSpeed * 2 / 3;
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
            playerController._useChargeJump = !disableChargeJump;
            playerController._runSpeed = defaultSpeed;
            playerController._strafeSpeed = strafeSpeed;
            playerController._walkSpeed = walkSpeed;
            playerController._maxJumpSpeed = jumpPower;
            ModHelper.Events.Unity.FireInNUpdates(() =>
                animController._animator.speed = Mathf.Max(defaultSpeed / 6, 1), 60);
        }

        // Every frame
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
                    bool startWalking;
                    bool stopWalking;
                    // We use the same controls as the downward thrust input for the jetpack - xen
                    startSprint = OWInput.IsNewlyPressed(InputLibrary.thrustDown);
                    stopSprint = OWInput.IsNewlyReleased(InputLibrary.thrustDown);
                    startWalking = OWInput.IsPressed(InputLibrary.rollMode);
                    stopWalking = OWInput.IsNewlyReleased(InputLibrary.rollMode);
                    // To sprint we have to be standing on the ground - xen
                    if ((startSprint && Locator.GetPlayerController().IsGrounded() && !OWInput.IsPressed(InputLibrary.rollMode))
                        || (stopWalking || StoppedFocusingLantern)
                            && OWInput.IsPressed(InputLibrary.thrustDown)
                            && Locator.GetPlayerController().IsGrounded())
                        StartSprinting();
                    if (stopSprint || startWalking)
                        StopSprinting();
                    if (startWalking)
                        animController._animator.speed = Mathf.Max(walkSpeed / 6, 1);
                    if (IsFocusingLantern)
                        animController._animator.speed = 1f;
                }
            }
        }

        // Sprinting functions
        public void StartSprinting()
        {
            if (enableSprint != true) return;
            IsDownThrustDisabled = true;
            playerController._runSpeed = sprintSpeed;
            playerController._strafeSpeed = sprintSpeed / defaultSpeed * strafeSpeed;
            animController._animator.speed = Mathf.Max(sprintSpeed / 6, 1);
        }

        public void StopSprinting()
        {
            if (enableSprint != true) return;
            IsDownThrustDisabled = false;
            playerController._runSpeed = defaultSpeed;
            playerController._strafeSpeed = strafeSpeed;
            animController._animator.speed = Mathf.Max(defaultSpeed / 6, 1);
        }
    }

    public static class Patches
    {
        // To disable the jetpack downward thrust while on the ground we have to patch the GetRawInput method in the JetpackThrusterController class - xen
        public static void JetpackThrusterControllerGetRawInput(ref Vector3 __result)
        {
            // When grounded, don't let the input have any downward component in the y-direction - xen
            if (HikersMod.Instance.IsDownThrustDisabled)
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
                if (OWInput.IsPressed(InputLibrary.thrustDown) && !OWInput.IsPressed(InputLibrary.rollMode))
                {
                    // This is where the Instance variable we made comes in use - xen
                    HikersMod.Instance.StartSprinting();
                }
            };
        }
        
        public static void DreamLanternFocusChanged(DreamLanternItem __instance)
        {
            HikersMod.Instance.StoppedFocusingLantern = true;
            if (__instance._focusing == true)
                HikersMod.Instance.IsFocusingLantern = true;
            else if (HikersMod.Instance.IsFocusingLantern == true)
                HikersMod.Instance.IsFocusingLantern = false;
                HikersMod.Instance.StoppedFocusingLantern = true;
        }
    }
}