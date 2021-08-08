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


		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.Mass"/>
		/// </summary>
		public static Identification Mass { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.LinearDamping"/>
		/// </summary>
		public static Identification LinearDamping { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.Force"/>
		/// </summary>
		public static Identification Force { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.Accleration"/>
		/// </summary>
		public static Identification Accleration { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.Velocity"/>
		/// </summary>
		public static Identification Velocity { get; internal set; }


		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.Inertia"/>
		/// </summary>
		public static Identification Inertia { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.AngularDamping"/>
		/// </summary>
		public static Identification AngularDamping { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.Torgue"/>
		/// </summary>
		public static Identification Torgue { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.AngularAccleration"/>
		/// </summary>
		public static Identification AngularAccleration { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Components.Common.Physic.Dynamics.AngularVelocity"/>
		/// </summary>
		public static Identification AngularVelocity { get; internal set; }
		public static Identification Gravity { get; internal set; }
		public static Identification Spring { get; internal set; }
	}
}