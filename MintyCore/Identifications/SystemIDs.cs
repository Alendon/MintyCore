using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.Identifications
{
	/// <summary>
	/// <see langword="static"/> partial class which contains all <see cref="ECS.ASystem"/> ids
	/// </summary>
	public static partial class SystemIDs
	{
		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Common.ApplyTransformSystem"/>
		/// </summary>
		public static Identification ApplyTransform {get; internal set;}

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Common.MovementSystem"/>
		/// </summary>
		public static Identification Movement { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Common.RotatorTestSystem"/>
		/// </summary>
		public static Identification Rotator { get; internal set; }


		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Client.RenderMeshSystem"/>
		/// </summary>
		public static Identification RenderMesh {get; internal set;}

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Client.InputSystem"/>
		/// </summary>
		public static Identification Input { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Client.RenderWireFrameSystem"/>
		/// </summary>
		public static Identification RenderWireFrame { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Client.IncreaseFrameNumberSystem"/>
		/// </summary>
		public static Identification IncreaseFrameNumber { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Client.ApplyGPUTransformBufferSystem"/>
		/// </summary>
		public static Identification ApplyGPUTransformBuffer { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the <see cref="Systems.Client.ApplyGPUCameraBufferSystem"/>
		/// </summary>
		public static Identification ApplyGPUCameraBuffer { get; internal set; }
		public static Identification CalculateLinearVelocity { get; internal set; }
		public static Identification CalculateLinearAccleration { get; internal set; }
		public static Identification CalculateAngularVelocity { get; internal set; }
		public static Identification CalculateAngularAccleration { get; internal set; }
		public static Identification CalculatePosition { get; internal set; }
		public static Identification CalculateRotation { get; internal set; }
		public static Identification SpringGenerator { get; internal set; }
		public static Identification GravityGenerator { get; internal set; }
	}
}
