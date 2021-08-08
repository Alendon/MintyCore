using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic.Dynamics
{
	/// <summary>
	/// Component that describes the change of Velocity
	/// </summary>
	struct Accleration : IComponent
	{
		public Vector3 Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Accleration;

		public void Deserialize(DataReader reader)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public void PopulateWithDefaultValues()
		{
			throw new NotImplementedException();
		}

		public void Serialize(DataWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}
