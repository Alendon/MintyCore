using MintyCore.Modding;
using MintyCore.Utils;
using TestMod;

namespace TestSubMod
{
    public class SimpleSubtestMod : IMod
    {
        public void Dispose()
        {
        }

        public ushort ModId { get; set; }
        public string StringIdentifier => "test_sub";
        public string ModDescription => "Just a little sub mod test";
        public string ModName => "Sub Mod Test";

        public ModVersion ModVersion => new ModVersion(0, 0, 1);
        public ModDependency[] ModDependencies => new[] { new ModDependency("test", new ModVersion(0, 0, 1)) };
        public GameType ExecutionSide => GameType.LOCAL;

        public void PreLoad()
        {
        }

        public void Load()
        {
            Logger.WriteLog($"Fetched Number: {SimpleTestMod.RandomNumber}", LogImportance.INFO, "TestSubMod");
        }

        public void PostLoad()
        {
        }

        public void Unload()
        {
        }
    }
}