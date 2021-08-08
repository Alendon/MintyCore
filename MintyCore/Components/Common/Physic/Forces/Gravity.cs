using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic.Forces
{
	struct Gravity : IComponent
	{
		public Vector3 Value;

		public byte Dirty {  get; set; }

		public Identification Identification => ComponentIDs.Gravity;

		public void Deserialize(DataReader reader)
		{
			Value = reader.GetVector3();
		}

		public void Dispose()
		{
		}

		public void PopulateWithDefaultValues()
		{
			Value = new(0, -9.81f, 0);
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(Value);
		}
	}
}
