using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Identifications
{
	/// <summary>
	/// <see langword="static"/> partial class which contains all <see cref="Render.Mesh"/> ids
	/// </summary>
	public static partial class MeshIDs
	{
		/// <summary>
		/// <see cref="Identification"/> of the Suzanne mesh
		/// </summary>
		public static Identification Suzanne { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the Square mesh
		/// </summary>
		public static Identification Square { get; internal set;  }
		public static Identification Capsule { get; internal set; }
		public static Identification Cube { get; internal set; }
		public static Identification Sphere { get; internal set; }
	}
}
