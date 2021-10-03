namespace MintyCore.Modding
{
    /// <summary>
    /// Struct to represent a mod dependency
    /// </summary>
    public readonly struct ModDependency
    {
        /// <summary>
        /// String identifier of the dependent mod
        /// </summary>
        public readonly string StringIdentifier;
        
        /// <summary>
        /// Version of the dependency
        /// </summary>
        public readonly ModVersion ModVersion;

        /// <summary>
        /// Create a new dependency
        /// </summary>
        public ModDependency(string stringIdentifier, ModVersion modVersion)
        {
            StringIdentifier = stringIdentifier;
            ModVersion = modVersion;
        }
    }
}