using MintyCore;
using MintyCore.Modding;
using MintyCore.Utils;

namespace TestMod;

public sealed partial class Test : IMod
{
    public static Test Instance { get; private set; } = null!;
    
    public void Dispose()
    {
        //Nothing to do here
    }

    public ushort ModId { get; set; }
    public void PreLoad()
    {
        Instance = this;
        
        Engine.RunMainMenu = () =>
        {
            Logger.WriteLog("Welcome to the TestMod MainMenu!", LogImportance.Info, "TestMod");   
        };
        
        Engine.RunHeadless = () =>
        {
            Logger.WriteLog("Welcome to the TestMod Headless!", LogImportance.Info, "TestMod");   
        };
    }

    public void Load()
    {
        InternalRegister();
    }

    public void PostLoad()
    {
        //Nothing to do here
    }

    public void Unload()
    {
        InternalUnregister();
    }
}