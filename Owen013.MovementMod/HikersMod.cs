using HarmonyLib;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;

namespace HikersMod
{
    public class HikersMod : ModBehaviour
    {
        // Config vars
        public bool instantJumpEnabled, slowStrafeDisabled, moreAirControlEnabled, floatyPhysicsEnabled, climbingEnabled, superBoostEnabled;
        public float runSpeed, walkSpeed, groundAccel, airSpeed, airAccel, jumpPower, jetpackAccel, jetpackBoostAccel, jetpackBoostTime, sprintSpeed, wallJumpsPerClimb, floatyPhysicsPower, superBoostPower;
        public string sprintEnabled;

        // Mod vars
        public PlayerCharacterController characterController;
        private PlayerAnimController animController;
        private PlayerAudioController audioController;
        private PlayerImpactAudio impactAudio;
        private JetpackThrusterController jetpackController;
        private JetpackThrusterModel jetpackModel;
        private OWAudioSource superBoostAudio;
        private ThrusterFlameController downThrustFlame;
        public bool allLoaded, superBoosting, isDreaming;
        public float runAnimSpeed, sprintAnimSpeed, walkAnimSpeed, strafeSpeed, sprintStrafeSpeed, wallJumpsLeft, lastWallJumpTime, lastWallJumpRefill, lastBoostInputTime, lastBoostTime;
        private MoveSpeed moveSpeed;
        ISmolHatchling smolHatchlingAPI;

        // Patch vars
        public static HikersMod Instance;
        public static bool disableDownThrust, dreamLanternFocused, dreamLanternFocusChanged;

        public void Awake()
        {
            // Static reference to HikersMod so it can be used in patches.
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Patches));
        }

        public void Start()
        {
            // Get Smol Hatchling
            smolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
            if (smolHatchlingAPI != null)
            {
                smolHatchlingAPI.SetHikersModEnabled();
            }
            // Ready!
            ModHelper.Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
        }

        public void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (WrongScene() || !allLoaded) return;
            // If the input changes for rollmode or thrustdown, or if the dream lantern focus just changed, then call UpdateMoveSpeed()
            if (OWInput.IsNewlyPressed(InputLibrary.rollMode) || OWInput.IsNewlyPressed(InputLibrary.thrustDown) || OWInput.IsNewlyReleased(InputLibrary.rollMode) || OWInput.IsNewlyReleased(InputLibrary.thrustDown) || dreamLanternFocusChanged) UpdateMoveSpeed();
            UpdateClimbing();
            UpdateAnimSpeed();
            UpdateSuperBoost();
            if (floatyPhysicsEnabled) UpdateAcceleration();
            dreamLanternFocusChanged = false;
        }

        public override void Configure(IModConfig config)
        {
            base.Configure(config);
            runSpeed = config.GetSettingsValue<float>("Normal Speed (Default 6)");
            walkSpeed = config.GetSettingsValue<float>("Walk Speed (Default 3)");
            groundAccel = config.GetSettingsValue<float>("Ground Acceleration (Default 0.5)");
            airSpeed = config.GetSettingsValue<float>("Air Speed (Default 3)");
            airAccel = config.GetSettingsValue<float>("Air Acceleration (Default 0.09)");
            jumpPower = config.GetSettingsValue<float>("Jump Power (Default 7)");
            instantJumpEnabled = config.GetSettingsValue<bool>("Instant Jump");
            jetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration (Default 6)");
            jetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration (Default 23)");
            jetpackBoostTime = config.GetSettingsValue<float>("Jetpack Boost Seconds until Depletion (Default 1)");
            slowStrafeDisabled = config.GetSettingsValue<bool>("Disable Strafing Slowdown");
            moreAirControlEnabled = config.GetSettingsValue<bool>("More Air Control");
            sprintEnabled = config.GetSettingsValue<string>("Enable Sprinting");
            sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
            climbingEnabled = config.GetSettingsValue<bool>("Enable Climbing");
            wallJumpsPerClimb = config.GetSettingsValue<float>("Wall Jumps per Climb");
            floatyPhysicsEnabled = config.GetSettingsValue<bool>("Floaty Physics in Low-Gravity");
            floatyPhysicsPower = config.GetSettingsValue<float>("Floaty Physics Power");
            superBoostEnabled = config.GetSettingsValue<bool>("Enable Jetpack Super-Boost");
            superBoostPower = config.GetSettingsValue<float>("Super-Boost Power");
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

            UpdateAttributes();
        }

        public void OnCharacterStart()
        {
            // Get vars
            characterController = Locator.GetPlayerController();
            animController = FindObjectOfType<PlayerAnimController>();
            audioController = FindObjectOfType<PlayerAudioController>();
            impactAudio = FindObjectOfType<PlayerImpactAudio>();
            jetpackController = FindObjectOfType<JetpackThrusterController>();
            jetpackModel = FindObjectOfType<JetpackThrusterModel>();
            var thrusters = Resources.FindObjectsOfTypeAll<ThrusterFlameController>();
            for (int i = 0; i < thrusters.Length; i++) if (thrusters[i]._thruster == Thruster.Up_LeftThruster) downThrustFlame = thrusters[i];

            characterController.OnBecomeGrounded += () =>
            {
                UpdateMoveSpeed();
                wallJumpsLeft = wallJumpsPerClimb;
                superBoosting = false;
            };

            // Create superboost audio source
            superBoostAudio = new GameObject("HikersMod_SuperBoostAudioSrc").AddComponent<OWAudioSource>();
            superBoostAudio.transform.parent = audioController.transform;
            superBoostAudio.transform.localPosition = new Vector3(0, 0, 1);

            isDreaming = false;

            // The Update() code won't run until after Setup() has at least once
            allLoaded = true;

            UpdateAttributes();
        }

        public void UpdateAttributes()
        {
            if (WrongScene() || !allLoaded) return;
            // Change built-in character attributes
            characterController._useChargeJump = !instantJumpEnabled;
            characterController._runSpeed = runSpeed;
            characterController._strafeSpeed = strafeSpeed;
            characterController._walkSpeed = walkSpeed;
            if (!floatyPhysicsEnabled) characterController._acceleration = groundAccel;
            characterController._airSpeed = airSpeed;
            characterController._airAcceleration = airAccel;
            characterController._maxJumpSpeed = jumpPower;
            jetpackModel._maxTranslationalThrust = jetpackAccel;
            jetpackModel._boostThrust = jetpackBoostAccel;
            jetpackModel._boostSeconds = jetpackBoostTime;
            // Set moveState to normal
            UpdateMoveSpeed();
        }

        public void UpdateMoveSpeed()
        {
            bool grounded = characterController._isGrounded;
            bool holdingLantern = characterController._heldLanternItem != null;
            bool walking = (OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern) || dreamLanternFocused;
            bool canSprint = ((sprintEnabled == "Everywhere") || sprintEnabled == "Real World Only" && !isDreaming) && !walking;
            bool sprintKeyHeld = OWInput.IsPressed(InputLibrary.thrustDown);
            if (canSprint && grounded && sprintKeyHeld)
            {
                moveSpeed = MoveSpeed.Sprinting;
                characterController._runSpeed = sprintSpeed;
                characterController._strafeSpeed = sprintStrafeSpeed;
                animController._animator.speed = sprintAnimSpeed;
                disableDownThrust = true;
            }
            else if (walking)
            {
                moveSpeed = MoveSpeed.Walking;
                if (dreamLanternFocused) animController._animator.speed = 1;
                else animController._animator.speed = walkAnimSpeed;
                disableDownThrust = false;
            }
            else
            {
                moveSpeed = MoveSpeed.Normal;
                characterController._runSpeed = runSpeed;
                characterController._strafeSpeed = strafeSpeed;
                animController._animator.speed = runAnimSpeed;
                disableDownThrust = false;
            }

            PrintLog("Just updated movement speed");
        }

        public void UpdateAcceleration()
        {
            float gravMultiplier;
            if (characterController.IsGrounded()) gravMultiplier = Mathf.Min(Mathf.Pow(characterController.GetNormalAccelerationScalar() / 12, floatyPhysicsPower), 1);
            else gravMultiplier = 1;
            characterController._acceleration = groundAccel * gravMultiplier;
        }

        public void UpdateAnimSpeed()
        {
            float sizeMultiplier;
            if (smolHatchlingAPI != null) sizeMultiplier = smolHatchlingAPI.GetAnimSpeed();
            else sizeMultiplier = 1;
            float gravMultiplier = characterController._acceleration / groundAccel;
            float multiplier = sizeMultiplier * gravMultiplier;
            switch (moveSpeed)
            {
                case MoveSpeed.Normal:
                    animController._animator.speed = Mathf.Max(runSpeed / 6 * multiplier, multiplier);
                    break;
                case MoveSpeed.Walking:
                    if (dreamLanternFocused) animController._animator.speed = multiplier;
                    else animController._animator.speed = Mathf.Max(walkSpeed / 6 * multiplier, multiplier);
                    break;
                case MoveSpeed.Sprinting:
                    animController._animator.speed = Mathf.Max(sprintSpeed / 6 * multiplier, multiplier);
                    break;
            }
        }

        public void UpdateClimbing()
        {
            bool grounded = characterController._isGrounded;
            characterController.UpdatePushable();
            bool canClimb = characterController._isPushable && !PlayerState.InZeroG() && !grounded;
            bool jumpKeyPressed = OWInput.IsNewlyPressed(InputLibrary.jump);
            if (climbingEnabled && canClimb && jumpKeyPressed && wallJumpsLeft > 0) Climb();
            // Replenish 1 wall jump if the player hasn't done one for five seconds
            if (Time.time - lastWallJumpRefill > 5 && wallJumpsLeft < wallJumpsPerClimb)
            {
                wallJumpsLeft += 1;
                lastWallJumpRefill = Time.time;
            }
            // Make player play fast freefall animation for one second after each wall jump
            if (Time.time - lastWallJumpTime < 1) animController._animator.SetFloat("FreefallSpeed", 100);
        }

        public void Climb()
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

        public void UpdateSuperBoost()
        {
            if (characterController.IsGrounded() || !characterController._isWearingSuit || PlayerState.InZeroG() || PlayerState.IsInsideShip()) superBoosting = false;
            else if (OWInput.IsNewlyPressed(InputLibrary.jump) && Time.time - lastBoostInputTime < 0.25f && superBoostEnabled && !superBoosting)
            {
                lastBoostTime = Time.time;
                superBoosting = true;
                jetpackModel._boostChargeFraction = 1f;
                superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, 1f);
            }
            if (OWInput.IsNewlyPressed(InputLibrary.jump) && !characterController.IsGrounded()) lastBoostInputTime = Time.time;
            if (superBoosting)
            {
                if (jetpackModel._boostChargeFraction > 0)
                {
                    jetpackModel._boostActivated = true;
                    jetpackController._translationalInput.y = 1;
                }
                else
                {
                    jetpackController._translationalInput.y = 0;
                }
                jetpackModel._boostThrust = jetpackBoostAccel * superBoostPower;
                jetpackModel._boostSeconds = jetpackBoostTime / superBoostPower;
                jetpackModel._chargeSeconds = float.PositiveInfinity;
                downThrustFlame._currentScale = Mathf.Max(downThrustFlame._currentScale, Mathf.Max(2 - (Time.time - lastBoostTime), 0) * 7.5f * jetpackModel._boostChargeFraction);
            }
            else
            {
                jetpackModel._boostThrust = jetpackBoostAccel;
                jetpackModel._boostSeconds = jetpackBoostTime;
                if (characterController.IsGrounded()) jetpackModel._chargeSeconds = jetpackModel._chargeSecondsGround;
                else jetpackModel._chargeSeconds = jetpackModel._chargeSecondsAir;
            }
        }

        public bool WrongScene()
        {
            OWScene scene = LoadManager.s_currentScene;
            return !(scene == OWScene.SolarSystem || scene == OWScene.EyeOfTheUniverse);
        }

        public void PrintLog(string text)
        {
            ModHelper.Console.WriteLine(text);
        }

        public void PrintLog(string text, MessageType type)
        {
            ModHelper.Console.WriteLine(text, type);
        }
    }
}