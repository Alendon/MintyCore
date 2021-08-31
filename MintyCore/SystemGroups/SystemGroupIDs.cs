using MintyCore.Utils;

namespace MintyCore.SystemGroups
{
	/// <summary>
	///     Collection of <see cref="Identification" /> for <see cref="ECS.ASystemGroup" />
	/// </summary>
	public static class SystemGroupIDs
    {
	    /// <summary>
	    ///     <see cref="Identification" /> for <see cref="InitializationSystemGroup" />
	    /// </summary>
	    public static Identification Initialization;

	    /// <summary>
	    ///     <see cref="Identification" /> for <see cref="SimulationSystemGroup" />
	    /// </summary>
	    public static Identification Simulation;

	    /// <summary>
	    ///     <see cref="Identification" /> for <see cref="FinalizationSystemGroup" />
	    /// </summary>
	    public static Identification Finalization;

	    /// <summary>
	    ///     <see cref="Identification" /> for <see cref="PresentationSystemGroup" />
	    /// </summary>
	    public static Identification Presentation;

	    /// <summary>
	    ///     <see cref="Identification" /> for <see cref="PhysicSystemGroup" />
	    /// </summary>
	    public static Identification Physic { get; internal set; }
    }
}