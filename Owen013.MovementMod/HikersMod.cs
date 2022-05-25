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
        private bool chargeJumpDisabled, slowStrafeDisabled, sprintEnabled, wallJumpEnabled;
        public float runSpeed, walkSpeed, jumpPower, sprintSpeed, wallJumpsPerClimb;

        // Mod vars
        private PlayerCharacterController characterController;
        private PlayerCameraController cameraController;
        private PlayerAnimController animController;
        private PlayerImpactAudio impactAudio;
        private bool allLoaded;
        public float runAnimSpeed, sprintAnimSpeed, walkAnimSpeed, strafeSpeed, sprintStrafeSpeed, wallJumpsLeft,
                     lastWallJumpTime, lastWallJumpRefill;
        private string moveState;

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
            wallJumpEnabled = config.GetSettingsValue<bool>("Enable Climbing");
            wallJumpsPerClimb = config.GetSettingsValue<float>("Wall Jumps per Climb");
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
            impactAudio = FindObjectOfType<PlayerImpactAudio>();

            //button_enableSprint = GameObject.Find("Enable Sprinting").gameObject.GetComponent<MenuOption>();
            //button_sprintSpeed = GameObject.Find("Sprint Speed").gameObject.GetComponent<MenuOption>();
            //button_enableClimb = GameObject.Find("Enable Climbing").gameObject.GetComponent<MenuOption>();
            //button_jumpsPerClimb = GameObject.Find("Climb Jumps per Jump").gameObject.GetComponent<MenuOption>();

            runAnimSpeed = Mathf.Max(runSpeed / 6 * animSpeed, animSpeed);
            sprintAnimSpeed = Mathf.Max(sprintSpeed / 6 * animSpeed, animSpeed);
            walkAnimSpeed = Mathf.Max(walkSpeed / 6 * animSpeed, animSpeed);

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

            // The Update() code won't run until after Setup() has at least once
            allLoaded = true;

            // Change built-in character attributes
            characterController._useChargeJump = !chargeJumpDisabled;
            characterController._runSpeed = runSpeed;
            characterController._strafeSpeed = strafeSpeed;
            characterController._walkSpeed = walkSpeed;
            characterController._maxJumpSpeed = jumpPower;
            // Set moveState to normal
            SetMoveState("Normal");
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
            if (moveState != "Sprinting" && canSprint && grounded && sprintKeyHeld) SetMoveState("Sprinting");
            else if (moveState != "Walking" && walking) SetMoveState("Walking");
            else if (moveState != "Normal" && !sprintKeyHeld && !walking) SetMoveState("Normal");

            // Climbing
            characterController.UpdatePushable();
            bool canClimb = characterController._isPushable && !characterController._isZeroGMovementEnabled && !grounded;
            bool jumpKeyPressed = OWInput.IsNewlyPressed(InputLibrary.jump);
            if (wallJumpEnabled && canClimb && jumpKeyPressed && wallJumpsLeft > 0) Climb();
            // Replenish 1 wall jump if the player hasn't done one for five seconds
            if (Time.time - lastWallJumpRefill > 5 && wallJumpsLeft < wallJumpsPerClimb)
            {
                wallJumpsLeft += 1;
                lastWallJumpRefill = Time.time;
            }
            // Make player play fast freefall animation for one second after each wall jump
            if (Time.time - lastWallJumpTime < 1) animController._animator.SetFloat("FreefallSpeed", 100);
        }

        public void SetMoveState(string state)
        {
            moveState = state;
            switch (state)
            {
                case "Normal":
                    characterController._runSpeed = runSpeed;
                    characterController._strafeSpeed = strafeSpeed;
                    animController._animator.speed = runAnimSpeed;
                    disableDownThrust = false;
                    break;
                case "Walking":
                    animController._animator.speed = walkAnimSpeed;
                    disableDownThrust = false;
                    break;
                case "Sprinting":
                    characterController._runSpeed = sprintSpeed;
                    characterController._strafeSpeed = sprintStrafeSpeed;
                    animController._animator.speed = sprintAnimSpeed;
                    disableDownThrust = true;
                    break;
            }
        }

        private void Climb()
        {
            OWRigidbody pushBody = characterController._pushableBody;
            Vector3 pushPoint = characterController._pushContactPt;
            Vector3 pointVelocity = pushBody.GetPointVelocity(pushPoint);
            Vector3 climbVelocity = new Vector3(0, jumpPower * (wallJumpsLeft / wallJumpsPerClimb), 0);

            if ((pointVelocity - characterController._owRigidbody.GetVelocity()).magnitude > 20) return;

            characterController._owRigidbody.SetVelocity(pointVelocity);
            characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
            wallJumpsLeft -= 1;
            impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
            lastWallJumpTime = lastWallJumpRefill = Time.time;
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
                    HikersMod.Instance.SetMoveState("Sprinting");

                HikersMod.Instance.wallJumpsLeft = HikersMod.Instance.wallJumpsPerClimb;
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