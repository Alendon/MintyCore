using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.Identifications
{
	public static partial class SystemIDs
	{
		public static Identification ApplyTransform {get; internal set;}
		
		public static Identification TransformToMeshMatrixBuffer {get; internal set;}
		public static Identification ApplyMeshMatrixBuffer {get; internal set;}
		public static Identification RenderMesh {get; internal set;}
		public static Identification Input { get; internal set; }
		public static Identification Movement { get; internal set; }
		public static Identification Rotator { get; internal set; }
		public static Identification RenderWireFrame { get; internal set; }
		public static Identification IndirectRenderMesh { get; internal set; }
		public static Identification IncreaseFrameNumber { get; internal set; }
		public static Identification ApplyGPUTransformBuffer { get; internal set; }
		public static Identification ApplyGPUCameraBuffer { get; internal set; }
	}
}
