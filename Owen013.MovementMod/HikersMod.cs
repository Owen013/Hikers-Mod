using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using System.Net.Security;

namespace HikersMod
{
    public interface ISmolHatchling
    {
        float GetAnimSpeed();
        void SetHikersModEnabled();
    }

    public class HikersMod : ModBehaviour
    {
        // Config vars
        public bool chargeJumpDisabled, slowStrafeDisabled, floatyPhysicsEnabled, sprintEnabled, wallJumpEnabled;
        public float runSpeed, walkSpeed, jumpPower, groundAccel, airAccel, floatyPhysicsPower, sprintSpeed, wallJumpsPerClimb;

        // Mod vars
        private OWRigidbody playerBody;
        private PlayerCharacterController characterController;
        private PlayerCameraController cameraController;
        private PlayerAnimController animController;
        private PlayerImpactAudio impactAudio;
        private bool allLoaded;
        public float runAnimSpeed, sprintAnimSpeed, walkAnimSpeed, strafeSpeed, sprintStrafeSpeed, wallJumpsLeft, lastWallJumpTime, lastWallJumpRefill;
        private string moveState;
        ISmolHatchling smolHatchlingAPI;

        // Patch vars
        public static HikersMod Instance;
        public static bool disableDownThrust, dreamLanternFocused;

        public override void Configure(IModConfig config)
        {
            base.Configure(config);
            chargeJumpDisabled = config.GetSettingsValue<bool>("Disable Charge-Jump");
            slowStrafeDisabled = config.GetSettingsValue<bool>("Disable Slow Strafing");
            runSpeed = config.GetSettingsValue<float>("Normal Speed (Default 6)");
            walkSpeed = config.GetSettingsValue<float>("Walk Speed (Default 3)");
            jumpPower = config.GetSettingsValue<float>("Jump Power (Default 7)");
            groundAccel = config.GetSettingsValue<float>("Ground Acceleration (Default 0.5)");
            airAccel = config.GetSettingsValue<float>("Air Acceleration (Default 0.09)");
            floatyPhysicsEnabled = config.GetSettingsValue<bool>("Floaty Physics in Low-Gravity");
            floatyPhysicsPower = config.GetSettingsValue<float>("Floaty Physics Power");
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
            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>("Start", typeof(Patches), nameof(Patches.CharacterStart));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>("Awake", typeof(Patches), nameof(Patches.CharacterAwake));
            ModHelper.HarmonyHelper.AddPostfix<JetpackThrusterController>("GetRawInput", typeof(Patches), nameof(Patches.GetJetpackInput));
            ModHelper.HarmonyHelper.AddPostfix<DreamLanternItem>("UpdateFocus", typeof(Patches), nameof(Patches.DreamLanternFocusChanged));
            // Ready!
            ModHelper.Console.WriteLine($"{nameof(HikersMod)} is ready to go!", MessageType.Success);
        }

        private void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (WrongScene() || !allLoaded) return;
            UpdateSprinting();
            UpdateClimbing();
            UpdateAnimSpeed();
            if (floatyPhysicsEnabled) UpdateAcceleration();
        }

        private void UpdateSprinting()
        {
            bool grounded = characterController._isGrounded;
            bool holdingLantern = characterController._heldLanternItem != null;
            bool walking = (OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern) || dreamLanternFocused;
            bool canSprint = sprintEnabled && !walking;
            bool sprintKeyHeld = OWInput.IsPressed(InputLibrary.thrustDown);
            if (moveState != "Sprinting" && canSprint && grounded && sprintKeyHeld) SetMoveState("Sprinting");
            else if (moveState != "Walking" && walking) SetMoveState("Walking");
            else if (moveState != "Normal" && !sprintKeyHeld && !walking) SetMoveState("Normal");
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

        public void UpdateAnimSpeed()
        {
            float sizeMultiplier;
            if (smolHatchlingAPI != null) sizeMultiplier = smolHatchlingAPI.GetAnimSpeed();
            else sizeMultiplier = 1;
            float gravMultiplier = characterController._acceleration / groundAccel;
            float multiplier = sizeMultiplier * gravMultiplier;
            switch (moveState)
            {
                case "Normal":
                    animController._animator.speed = Mathf.Max(runSpeed / 6 * multiplier, multiplier);
                    break;
                case "Walking":
                    animController._animator.speed = Mathf.Max(walkSpeed / 6 * multiplier, multiplier);
                    break;
                case "Sprinting":
                    animController._animator.speed = Mathf.Max(sprintSpeed / 6 * multiplier, multiplier);
                    break;
            }
        }

        public void UpdateAcceleration()
        {
            float gravMultiplier;
            if (characterController.IsGrounded()) gravMultiplier = Mathf.Min(Mathf.Pow(characterController.GetNormalAccelerationScalar() / 12, floatyPhysicsPower), 1);
            else gravMultiplier = 1;
            characterController._acceleration = groundAccel * gravMultiplier;
        }

        private void UpdateClimbing()
        {
            bool grounded = characterController._isGrounded;
            characterController.UpdatePushable();
            bool canClimb = characterController._isPushable && !PlayerState.InZeroG() && !grounded;
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

        public void Setup()
        {
            // Make sure that the scene is the SS or Eye
            if (WrongScene()) return;
            
            // Get Smol Hatchling
            smolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
            if (smolHatchlingAPI != null)
            {
                smolHatchlingAPI.SetHikersModEnabled();
            }

            // Get vars
            playerBody = Locator.GetPlayerBody();
            characterController = Locator.GetPlayerController();
            cameraController = Locator.GetPlayerCameraController();
            animController = FindObjectOfType<PlayerAnimController>();
            impactAudio = FindObjectOfType<PlayerImpactAudio>();

            //button_enableSprint = GameObject.Find("Enable Sprinting").gameObject.GetComponent<MenuOption>();
            //button_sprintSpeed = GameObject.Find("Sprint Speed").gameObject.GetComponent<MenuOption>();
            //button_enableClimb = GameObject.Find("Enable Climbing").gameObject.GetComponent<MenuOption>();
            //button_jumpsPerClimb = GameObject.Find("Climb Jumps per Jump").gameObject.GetComponent<MenuOption>();

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
            if (!floatyPhysicsEnabled) characterController._acceleration = groundAccel;
            characterController._airAcceleration = airAccel;
            // Set moveState to normal
            SetMoveState("Normal");
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
                if (OWInput.IsPressed(InputLibrary.thrustDown) && !OWInput.IsPressed(InputLibrary.rollMode) && HikersMod.Instance.sprintEnabled)
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