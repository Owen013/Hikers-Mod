using HarmonyLib;
using HikersMod.Interfaces;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace HikersMod;

class ModMain : ModBehaviour
{
    public static IModConsole ModConsole => s_instance?.ModHelper?.Console;

    public static IModConfig ModConfig => s_instance?.ModHelper?.Config;

    public static ISmolHatchling SmolHatchlingAPI { get; private set; }

    public static ICameraShaker CameraShakerAPI { get; private set; }

    static ModMain s_instance;

    public override object GetApi()
    {
        return new HikersModAPI();
    }

    public override void Configure(IModConfig config)
    {
        Config.Configure(config);
    }

    void Awake()
    {
        // Static reference to HikersMod so it can be used in patches.
        s_instance = this;
        new Harmony("Owen013.MovementMod").PatchAll(Assembly.GetExecutingAssembly());
    }

    void Start()
    {
        // Get APIs
        SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
        CameraShakerAPI = ModHelper.Interaction.TryGetModApi<ICameraShaker>("SBtT.CameraShake");

        // Ready!
        ModConsole?.WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }
}