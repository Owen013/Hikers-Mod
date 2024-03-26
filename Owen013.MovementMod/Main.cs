using HarmonyLib;
using HikersMod.APIs;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace HikersMod;

public class Main : ModBehaviour
{
    public static Main Instance { get; private set; }
    public ISmolHatchling SmolHatchlingAPI { get; private set; }
    public ICameraShaker CameraShakerAPI { get; private set; }

    public override void Configure(IModConfig config)
    {
        Config.UpdateConfig(config);
    }

    private void Awake()
    {
        // Static reference to HikersMod so it can be used in patches.
        Instance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        // Get APIs
        SmolHatchlingAPI = ModHelper.Interaction.TryGetModApi<ISmolHatchling>("Owen013.TeenyHatchling");
        CameraShakerAPI = ModHelper.Interaction.TryGetModApi<ICameraShaker>("SBtT.CameraShake");

        // Ready!
        WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }

    public static void WriteLine(string text, MessageType type = MessageType.Message)
    {
        // null check because this method is created before ModHelper is defined!
        if (Instance.ModHelper == null) return;

        Instance.ModHelper.Console.WriteLine(text, type);
    }
}