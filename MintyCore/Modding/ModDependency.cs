namespace MintyCore.Modding
{
    public readonly struct ModDependency
    {
        public readonly string StringIdentifier;
        public readonly ModVersion ModVersion;

        public ModDependency(string stringIdentifier, ModVersion modVersion)
        {
            StringIdentifier = stringIdentifier;
            ModVersion = modVersion;
        }
    }
}