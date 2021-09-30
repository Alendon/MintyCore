using System;
using MintyCore.Modding;
using MintyCore.Utils;

namespace TestMod
{
    public class SimpleTestMod : IMod
    {
        public void Dispose()
        {
            
        }

        public ushort ModId { get; set; }
        public string StringIdentifier => "test";
        public string ModDescription => "Just a mod to test the ModManager";
        public string ModName => "Test Mod";
        
        public ModVersion ModVersion => new ModVersion(0, 0, 1);
        public ModDependency[] ModDependencies => Array.Empty<ModDependency>();
        public GameType ExecutionSide => GameType.LOCAL;

        public static int randomNumber;
        
        public void PreLoad()
        {
            Random rnd = new();
            randomNumber = rnd.Next(1, 1000);
            Logger.WriteLog($"Generated Number: {randomNumber}", LogImportance.INFO, "TestMod");
        }

        public void Load()
        {
            Logger.WriteLog("Loaded", LogImportance.INFO, "TestMod");
        }

        public void PostLoad()
        {
            
        }

        public void Unload()
        {
            Logger.WriteLog("Unloaded", LogImportance.INFO, "TestMod");
        }
    }
}