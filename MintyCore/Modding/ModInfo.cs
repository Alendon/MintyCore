using MintyCore.Utils;

namespace MintyCore.Modding
{
    public readonly struct ModInfo
    {
        public readonly string ModFileLocation;
        public readonly string ModId;
        public readonly string ModName;
        public readonly string ModDescription;
        public readonly ModVersion ModVersion;
        public readonly ModDependency[] ModDependencies;
        public readonly GameType ExecutionSide;

        public ModInfo(string modFileLocation, string modId, string modName, string modDescription, ModVersion modVersion, ModDependency[] modDependencies, GameType executionSide)
        {
            ModFileLocation = modFileLocation;
            ModId = modId;
            ModName = modName;
            ModDescription = modDescription;
            ModVersion = modVersion;
            ModDependencies = modDependencies;
            ExecutionSide = executionSide;
        }
    }
}