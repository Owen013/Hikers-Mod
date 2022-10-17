using HarmonyLib;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;

namespace HikersMod
{
    public class HikersMod : ModBehaviour
    {
        // Config vars
        public bool debugLogEnabled, instantJumpEnabled, slowStrafeDisabled, enhancedAirControlEnabled, allowGroundThrustWithSprint, floatyPhysicsEnabled, superBoostEnabled;
        public float runSpeed, walkSpeed, dreamLanternSpeed, groundAccel, airSpeed, airAccel, jumpPower, jetpackAccel, jetpackBoostAccel, jetpackBoostTime, sprintSpeed, wallJumpsPerClimb, floatyPhysicsPower, superBoostPower;
        public string sprintEnabled, sprintButtonName, climbingEnabled;

        // Mod vars
        public static HikersMod Instance;
        public AssetBundle textAssets;
        private PlayerCharacterController characterController;
        private PlayerAnimController animController;
        private PlayerAudioController audioController;
        private PlayerImpactAudio impactAudio;
        private JetpackThrusterController jetpackController;
        public JetpackThrusterModel jetpackModel;
        private OWAudioSource superBoostAudio;
        private ThrusterFlameController downThrustFlame;
        public GameObject superBoostNote;
        private ISmolHatchling smolHatchlingAPI;
        private MoveSpeed moveSpeed;
        public IInputCommands sprintButton;
        public bool characterLoaded, disableUpDownThrust, dreamLanternFocused, dreamLanternFocusChanged, superBoosting, isDreaming;
        private float strafeSpeed, sprintStrafeSpeed, wallJumpsLeft, lastWallJumpTime, lastWallJumpRefill, lastBoostInputTime, lastBoostTime;

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
            if (smolHatchlingAPI != null) smolHatchlingAPI.SetHikersModEnabled();

            textAssets = ModHelper.Assets.LoadBundle("Assets/textassets");

            // Set characterLoaded to false whenever a new scene begins loading
            LoadManager.OnStartSceneLoad += (scene, loadScene) => characterLoaded = false;
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => PlaceSuperBoostNote();

            // Ready!
            ModHelper.Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
        }

        public void Update()
        {
            // Make sure that the scene is the SS or Eye and that everything is loaded
            if (!IsCorrectScene() || !characterLoaded) return;

            // If the input changes for rollmode or thrustdown, or if the dream lantern focus just changed, then call UpdateMoveSpeed()
            if (InputChanged(InputLibrary.rollMode) ||
                InputChanged(InputLibrary.thrustDown) ||
                InputChanged(InputLibrary.thrustUp) ||
                (OWInput.IsNewlyPressed(InputLibrary.boost) && !characterController.IsGrounded()) ||
                dreamLanternFocusChanged)
            {
                UpdateMoveSpeed();
            }
            
            // Update everthing else
            UpdateClimbing();
            UpdateSuperBoost();
            if (floatyPhysicsEnabled) UpdateAcceleration();
            dreamLanternFocusChanged = false;
        }

        public override void Configure(IModConfig config)
        {
            base.Configure(config);

            // Get all settings values
            debugLogEnabled = config.GetSettingsValue<bool>("Enable Debug Log");
            runSpeed = config.GetSettingsValue<float>("Normal Speed (Default 6)");
            walkSpeed = config.GetSettingsValue<float>("Walk Speed (Default 3)");
            dreamLanternSpeed = config.GetSettingsValue<float>("Focused Lantern Speed (Default 2)");
            groundAccel = config.GetSettingsValue<float>("Ground Acceleration (Default 0.5)");
            airSpeed = config.GetSettingsValue<float>("Air Speed (Default 3)");
            airAccel = config.GetSettingsValue<float>("Air Acceleration (Default 0.09)");
            jumpPower = config.GetSettingsValue<float>("Jump Power (Default 7)");
            instantJumpEnabled = config.GetSettingsValue<bool>("Instant Jump");
            jetpackAccel = config.GetSettingsValue<float>("Jetpack Acceleration (Default 6)");
            jetpackBoostAccel = config.GetSettingsValue<float>("Jetpack Boost Acceleration (Default 23)");
            jetpackBoostTime = config.GetSettingsValue<float>("Jetpack Boost Seconds until Depletion (Default 1)");
            slowStrafeDisabled = config.GetSettingsValue<bool>("Disable Strafing Slowdown");
            enhancedAirControlEnabled = config.GetSettingsValue<bool>("Enhanced Air Control");
            sprintEnabled = config.GetSettingsValue<string>("Enable Sprinting");
            sprintButtonName = config.GetSettingsValue<string>("Sprint Button");
            allowGroundThrustWithSprint = config.GetSettingsValue<bool>("Allow Thrusting on Ground with Sprinting Enabled");
            sprintSpeed = config.GetSettingsValue<float>("Sprint Speed");
            climbingEnabled = config.GetSettingsValue<string>("Enable Climbing");
            wallJumpsPerClimb = config.GetSettingsValue<float>("Wall Jumps per Climb");
            floatyPhysicsEnabled = config.GetSettingsValue<bool>("Floaty Physics in Low-Gravity");
            floatyPhysicsPower = config.GetSettingsValue<float>("Floaty Physics Power");
            superBoostEnabled = config.GetSettingsValue<bool>("Enable Jetpack Super-Boost");
            superBoostPower = config.GetSettingsValue<float>("Super-Boost Power");

            // Strafe speed depends on whether or not slowStrafeDisabled is true
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
            characterLoaded = true;
            PrintLog("Character loaded", MessageType.Info);

            UpdateAttributes();
        }

        public void UpdateAttributes()
        {
            if (!IsCorrectScene() || !characterLoaded) return;

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

            if (sprintButtonName == "Down Thrust") sprintButton = InputLibrary.thrustDown;
            else sprintButton = InputLibrary.thrustUp;

            if (superBoostNote != null) superBoostNote.SetActive(superBoostEnabled);

            UpdateMoveSpeed();
        }

        public void UpdateMoveSpeed()
        {
            bool holdingLantern = characterController._heldLanternItem != null;
            bool walking = (OWInput.IsPressed(InputLibrary.rollMode) && !holdingLantern);
            MoveSpeed oldSpeed = moveSpeed;

            if (OWInput.IsPressed(sprintButton) &&
                characterController._isGrounded &&
                !characterController.IsSlidingOnIce() &&
                !walking &&
                !dreamLanternFocused &&
                ((sprintEnabled == "Everywhere") || sprintEnabled == "Real World Only" && !isDreaming) &&
                (OWInput.GetAxisValue(InputLibrary.moveXZ).magnitude > 0 || !characterController._isWearingSuit || !allowGroundThrustWithSprint || moveSpeed == MoveSpeed.Sprinting))
            {
                moveSpeed = MoveSpeed.Sprinting;
                characterController._runSpeed = sprintSpeed;
                characterController._strafeSpeed = sprintStrafeSpeed;
                disableUpDownThrust = true;
            }
            else if (walking)
            {
                moveSpeed = MoveSpeed.Walking;
                disableUpDownThrust = false;
            }
            else if (dreamLanternFocused)
            {
                moveSpeed = MoveSpeed.DreamLantern;
                disableUpDownThrust = false;
            }
            else
            {
                moveSpeed = MoveSpeed.Normal;
                characterController._runSpeed = runSpeed;
                characterController._strafeSpeed = strafeSpeed;
                disableUpDownThrust = false;
            }

            UpdateAnimSpeed();
            if (moveSpeed != oldSpeed) PrintLog($"Changed movement speed to {moveSpeed}");
        }

        public void UpdateAcceleration()
        {
            float gravMultiplier;
            if (characterController.IsGrounded() && !characterController.IsSlidingOnIce()) gravMultiplier = Mathf.Min(Mathf.Pow(characterController.GetNormalAccelerationScalar() / 12, floatyPhysicsPower), 1);
            else gravMultiplier = 1;
            characterController._acceleration = groundAccel * gravMultiplier;
            UpdateAnimSpeed();
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
                    animController._animator.speed = Mathf.Max(walkSpeed / 6 * multiplier, multiplier);
                    break;
                case MoveSpeed.DreamLantern:
                    animController._animator.speed = Mathf.Max(dreamLanternSpeed / 6 * multiplier, multiplier);
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
            if (((climbingEnabled == "When Unsuited" && !PlayerState.IsWearingSuit()) || climbingEnabled == "Always") && canClimb && jumpKeyPressed && wallJumpsLeft > 0) DoWallJump();

            // Replenish 1 wall jump if the player hasn't done one for five seconds
            if (Time.time - lastWallJumpRefill > 5 && wallJumpsLeft < wallJumpsPerClimb)
            {
                wallJumpsLeft += 1;
                lastWallJumpRefill = Time.time;
            }

            // Make player play fast freefall animation for one second after each wall jump
            if (Time.time - lastWallJumpTime < 1) animController._animator.SetFloat("FreefallSpeed", 100);
        }

        public void DoWallJump()
        {
            OWRigidbody pushBody = characterController._pushableBody;
            Vector3 pushPoint = characterController._pushContactPt;
            Vector3 pointVelocity = pushBody.GetPointVelocity(pushPoint);
            Vector3 climbVelocity = new Vector3(0, jumpPower * (wallJumpsLeft / wallJumpsPerClimb), 0);

            if ((pointVelocity - characterController._owRigidbody.GetVelocity()).magnitude > 20) PrintLog("Can't Wall-Jump; going too fast");
            else
            {
                characterController._owRigidbody.SetVelocity(pointVelocity);
                characterController._owRigidbody.AddLocalVelocityChange(climbVelocity);
                wallJumpsLeft -= 1;
                impactAudio._impactAudioSrc.PlayOneShot(AudioType.ImpactLowSpeed);
                lastWallJumpTime = lastWallJumpRefill = Time.time;
                PrintLog("Wall-Jumped");
            }
        }

        public void UpdateSuperBoost()
        {
            bool isInputting = OWInput.IsNewlyPressed(InputLibrary.jump) && (!OWInput.IsPressed(InputLibrary.thrustUp));
            bool meetsCriteria = characterController._isWearingSuit && !PlayerState.InZeroG() && !PlayerState.IsInsideShip() && !PlayerState.IsCameraUnderwater();
            if (!meetsCriteria) superBoosting = false;
            else if (isInputting && meetsCriteria && jetpackController._resources.GetFuel() > 0 && Time.time - lastBoostInputTime < 0.25f && superBoostEnabled && !superBoosting)
            {
                lastBoostTime = Time.time;
                superBoosting = true;
                jetpackModel._boostChargeFraction = 1f;
                superBoostAudio.PlayOneShot(AudioType.ShipDamageShipExplosion, 1f);
                PrintLog("Super-Boosted");
            }
            if (isInputting && meetsCriteria) lastBoostInputTime = Time.time;
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

        public bool IsCorrectScene()
        {
            OWScene scene = LoadManager.s_currentScene;
            return (scene == OWScene.SolarSystem || scene == OWScene.EyeOfTheUniverse);
        }

        public bool InputChanged(IInputCommands input)
        {
            return OWInput.IsNewlyPressed(input) || OWInput.IsNewlyReleased(input);
        }

        public void PlaceSuperBoostNote()
        {
            if (GameObject.Find("Ship_Body") == null) return;
            GameObject notesObject = Instantiate(GameObject.Find("DeepFieldNotes_2"));
            notesObject.AddComponent<SuperBoostNote>();
        }

        public void PrintLog(string text)
        {
            if (!debugLogEnabled) return;
            ModHelper.Console.WriteLine(text);
        }

        public void PrintLog(string text, MessageType type)
        {
            if (!debugLogEnabled) return;
            ModHelper.Console.WriteLine(text, type);
        }
    }
}