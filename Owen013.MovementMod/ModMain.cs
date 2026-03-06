using HarmonyLib;
using HikersMod.Interfaces;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace HikersMod
{
    public class ModMain : ModBehaviour
    {
        public static ModMain Instance { get; private set; }

        public static IModConsole Console => Instance.ModHelper.Console;

        public static ISmolHatchling SmolHatchlingAPI { get; private set; }

        public static ICameraShaker CameraShakerAPI { get; private set; }

        public override object GetApi()
        {
            return new HikersModAPI();
        }

        public override void Configure(IModConfig config)
        {
            Config.Configure(config);
        }

        private void Awake()
        {
            // Static reference to HikersMod so it can be used in patches.
            Instance = this;
            new Harmony("Owen013.MovementMod").PatchAll(Assembly.GetExecutingAssembly());
        }

        private void Start()
        {
            // Get APIs
            SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
            CameraShakerAPI = ModHelper.Interaction.TryGetModApi<ICameraShaker>("SBtT.CameraShake");

            // Ready!
            Console.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
        }
    }
}