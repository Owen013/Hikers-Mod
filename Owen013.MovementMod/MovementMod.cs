using OWML.ModHelper;
using OWML.Common;
using UnityEngine.InputSystem;

namespace MovementMod
{
    public class MovementMod : ModBehaviour
    {
        private PlayerCharacterController playerController;
        private PlayerAnimController animController;

        private void Awake()
        {
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        private void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"{nameof(MovementMod)} is loaded!", MessageType.Success);

            // Example of accessing game code.
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;
                playerController = FindObjectOfType<PlayerCharacterController>();
                animController = FindObjectOfType<PlayerAnimController>();
                playerController._useChargeJump = false;
                playerController._strafeSpeed = 6f;
                ModHelper.Console.WriteLine($"MovementMod loaded!", MessageType.Success);
            };
        }
        private void Update()
        {
            if (LoadManager.s_currentScene != OWScene.SolarSystem && LoadManager.s_currentScene != OWScene.EyeOfTheUniverse) return;
            {
                if (!OWInput.IsInputMode(InputMode.Menu))
                {
                    bool startSprint = false;
                    bool stopSprint = false;
                    if (Keyboard.current != null)
                    {
                        startSprint = Keyboard.current[Key.LeftAlt].wasPressedThisFrame;
                        stopSprint = Keyboard.current[Key.LeftAlt].wasReleasedThisFrame;
                        if (startSprint)
                        {
                            playerController._runSpeed = 9f;
                            playerController._strafeSpeed = 9f;
                            animController._animator.speed = 1.5f;
                        };
                        if (stopSprint)
                        {
                            playerController._runSpeed = 6f;
                            playerController._strafeSpeed = 6f;
                            animController._animator.speed = 1f;
                        }
                    }
                }
            }
        }
    }
}
