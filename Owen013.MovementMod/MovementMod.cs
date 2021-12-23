using OWML.ModHelper;
using OWML.Common;

namespace MovementMod
{
    public class MovementMod : ModBehaviour
    {
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
                if (loadScene != OWScene.SolarSystem) return;
                var playerController = FindObjectOfType<PlayerCharacterController>();
                playerController._useChargeJump = false;
                playerController._strafeSpeed = 6f;
                ModHelper.Console.WriteLine($"MovementMod loaded!", MessageType.Success);
            };
        }
    }
}
