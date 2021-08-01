using MintyCore.Utils;

namespace MintyCore.Identifications
{
    /// <summary>
	/// <see langword="static"/> partial class which contains all <see cref="ECS.IComponent"/> ids
	/// </summary>
    public static partial class ComponentIDs
    {
        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Common.Position"/>
        /// </summary>
        public static Identification Position { get; internal set; }

        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Common.Rotation"/>
        /// </summary>
        public static Identification Rotation { get; internal set; }

        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Common.Scale"/>
        /// </summary>
        public static Identification Scale { get; internal set; }

        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Common.Transform"/>
        /// </summary>
        public static Identification Transform { get; internal set; }


        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Client.Renderable"/>
        /// </summary>
        public static Identification Renderable { get; internal set; }

        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Client.Camera"/>
        /// </summary>
		public static Identification Camera { get; internal set; }

        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Client.Input"/>
        /// </summary>
		public static Identification Input { get; internal set; }

        /// <summary>
        /// <see cref="Identification"/> of the <see cref="Components.Common.Rotator"/>
        /// </summary>
		public static Identification Rotator { get; internal set; }
	}
}