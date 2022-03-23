using System.Collections;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;

namespace MovementMod
{
    public class HikersMod : ModBehaviour
    {
        // Config vars
        public static bool chargeJumpDisabled, slowStrafeDisabled, sprintEnabled, climbEnabled;
        public static float runSpeed, walkSpeed, jumpPower, sprintSpeed, climbPower, lastClimbTime, climbsPerJump, climbCooldownTime;

        // Mod vars
        public static OWScene scene;
        public static PlayerCharacterController characterController;
        public static PlayerCameraController cameraController;
        public static PlayerAnimController animController;
        public static PlayerMovementAudio movementAudio;
        public static PlayerImpactAudio impactAudio;
        public static float runAnimSpeed, sprintAnimSpeed, walkAnimSpeed, strafeSpeed, sprintStrafeSpeed, climbsLeft;
        public static bool sprinting, wavingArmsAnim;

        // Patch vars
        public static HikersMod Instance;
        public static bool disableDownThrust, focusingLantern, justUnfocusedLantern;

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
            climbEnabled = config.GetSettingsValue<bool>("Enable Climbing");
            climbPower = config.GetSettingsValue<float>("Climb Jump Power");
            climbsPerJump = config.GetSettingsValue<float>("Climb Jumps per Jump");
            climbCooldownTime = config.GetSettingsValue<float>("Climb Jump Cooldown Time");
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
            ModHelper.HarmonyHelper.AddPostfix<JetpackThrusterController>(
                "GetRawInput",
                typeof(Patches),
                nameof(Patches.GetJetpackInput));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCharacterController>(
                "Awake",
                typeof(Patches),
                nameof(Patches.CharacterAwake));
            ModHelper.HarmonyHelper.AddPostfix<DreamLanternItem>(
                "UpdateFocus",
                typeof(Patches),
                nameof(Patches.DreamLanternFocusChanged));
            ModHelper.HarmonyHelper.AddPostfix<PlayerAnimController>(
                "LateUpdate",
                typeof(Patches),
                nameof(Patches.AnimControllerLateUpdate));
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => Setup();
            ModHelper.Console.WriteLine($"{nameof(HikersMod)} is ready to go!", MessageType.Success);
        }

        private void Update()
        {
            if (scene == OWScene.SolarSystem || scene == OWScene.EyeOfTheUniverse)
            {
                var state = Locator.GetPlayerController();
                // Sprinting
                if (sprintEnabled)
                {
                    bool canSprint = state._isGrounded && !OWInput.IsPressed(InputLibrary.rollMode);
                    bool sprintPressed = OWInput.IsPressed(InputLibrary.thrustDown);
                    bool startSprint = OWInput.IsNewlyPressed(InputLibrary.thrustDown);
                    bool stopSprint = OWInput.IsNewlyReleased(InputLibrary.thrustDown);
                    bool startWalk = OWInput.IsPressed(InputLibrary.rollMode);
                    bool stopWalk = OWInput.IsNewlyReleased(InputLibrary.rollMode);
                    if (canSprint && (startSprint || ((stopWalk || justUnfocusedLantern) && sprintPressed))) Sprint(true);
                    if (stopSprint || startWalk || focusingLantern) Sprint(false);
                    justUnfocusedLantern = false;
                }
                // Climbing
                if (climbEnabled)
                {
                    characterController.UpdatePushable();
                    bool tryingToJump = OWInput.IsNewlyPressed(InputLibrary.jump);
                    bool isGrounded = state.IsGrounded();
                    bool isZeroG = state._isZeroGMovementEnabled;
                    bool jumpCooldown = Time.time - characterController._lastJumpTime < climbCooldownTime;
                    bool climbCooldown = Time.time - lastClimbTime < climbCooldownTime;
                    bool canClimb = state._isPushable
                                 && climbsLeft > 0
                                 && !isGrounded
                                 && !isZeroG
                                 && !jumpCooldown
                                 && !climbCooldown;
                    if (tryingToJump && canClimb) Climb();
                }
                if (wavingArmsAnim) animController._animator.speed = 2f;
                else
                {
                    if (sprinting) animController._animator.speed = sprintAnimSpeed;
                    else animController._animator.speed = runAnimSpeed;
                }
            }
        }

        public static void Setup()
        {
            scene = LoadManager.s_currentScene;
            if (scene == OWScene.SolarSystem || scene == OWScene.EyeOfTheUniverse)
            {
                characterController = FindObjectOfType<PlayerCharacterController>();
                cameraController = FindObjectOfType<PlayerCameraController>();
                animController = FindObjectOfType<PlayerAnimController>();
                movementAudio = FindObjectOfType<PlayerMovementAudio>();
                impactAudio = FindObjectOfType<PlayerImpactAudio>();
                characterController._useChargeJump = !chargeJumpDisabled;
                characterController._runSpeed = runSpeed;
                characterController._strafeSpeed = strafeSpeed;
                characterController._walkSpeed = walkSpeed;
                characterController._maxJumpSpeed = jumpPower;
                runAnimSpeed = Mathf.Max(runSpeed / 6, 1);
                sprintAnimSpeed = Mathf.Max(sprintSpeed / 6, 1);
                walkAnimSpeed = Mathf.Max(walkSpeed / 6, 1);
                if (characterController._isGrounded || climbsLeft > climbsPerJump) climbsLeft = climbsPerJump;
            };
        }

        public void Sprint(bool sprinting)
        {
            HikersMod.sprinting = sprinting;
            if (sprinting)
            {
                characterController._runSpeed = sprintSpeed;
                characterController._strafeSpeed = sprintStrafeSpeed;
                animController._animator.speed = sprintAnimSpeed;
                disableDownThrust = true;
            }
            else
            {
                characterController._runSpeed = runSpeed;
                characterController._strafeSpeed = strafeSpeed;
                animController._animator.speed = runAnimSpeed;
                disableDownThrust = false;
            }
        }

        public void Climb()
        {
            float thisClimbPower = (climbPower * 0.5f) * (climbsLeft / climbsPerJump) + climbPower * 0.5f;
            Vector3 climbVelocity = new Vector3(0, thisClimbPower, 0);
            climbsLeft -= 1;
            lastClimbTime = Time.time;
            characterController._owRigidbody.SetVelocity(
                characterController._pushableBody.GetPointVelocity(characterController._pushContactPt));
            characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
            movementAudio.OnJump();
            StopCoroutine(AnimateClimb());
            StartCoroutine(AnimateClimb());
        }

        private IEnumerator AnimateClimb()
        {
            wavingArmsAnim = true;
            yield return new WaitForSeconds(0.5f);
            wavingArmsAnim = false;
        }
    }

    public static class Patches
    {
        public static void GetJetpackInput(ref Vector3 __result)
        {
            if (HikersMod.disableDownThrust && __result.y < 0) __result.y = 0;
        }

        public static void CharacterAwake(PlayerCharacterController __instance)
        {
            __instance.OnBecomeGrounded += () =>
            {
                HikersMod.climbsLeft = HikersMod.climbsPerJump;
                if (OWInput.IsPressed(InputLibrary.thrustDown) && !OWInput.IsPressed(InputLibrary.rollMode))
                    HikersMod.Instance.Sprint(true);
            };
        }

        public static void DreamLanternFocusChanged(DreamLanternItem __instance)
        {
            bool lanternIsFocused = __instance._focusing;
            // If the lantern was just unfocused...
            if (!lanternIsFocused && HikersMod.focusingLantern)
            {
                HikersMod.focusingLantern = false;
                HikersMod.justUnfocusedLantern = true;
            }
            else if (lanternIsFocused) HikersMod.focusingLantern = true;
        }

        public static void AnimControllerLateUpdate()
        {
            if (HikersMod.wavingArmsAnim) HikersMod.animController._animator.SetFloat("FreefallSpeed", 100f);
        }
    }
}