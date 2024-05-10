using HarmonyLib;
using HikersMod.Interfaces;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace HikersMod;

public class ModMain : ModBehaviour
{
    public static ModMain Instance { get; private set; }

    public ISmolHatchling SmolHatchlingAPI { get; private set; }

    public ICameraShaker CameraShakerAPI { get; private set; }
    
    public IImmersion ImmersionAPI { get; private set; }

    public override object GetApi()
    {
        return new HikersModAPI();
    }

    public override void Configure(IModConfig config)
    {
        Config.UpdateConfig(config);
    }

    public void WriteLine(string text, MessageType type = MessageType.Message)
    {
        Instance.ModHelper.Console.WriteLine(text, type);
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
        ImmersionAPI = ModHelper.Interaction.TryGetModApi<IImmersion>("Owen_013.FirstPersonPresence");

        // Ready!
        WriteLine($"Hiker's Mod is ready to go!", MessageType.Success);
    }
}