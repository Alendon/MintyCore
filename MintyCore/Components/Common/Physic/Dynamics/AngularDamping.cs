using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic.Dynamics
{
	struct AngularDamping : IComponent
	{
		public float Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.AngularDamping;

		public void Deserialize(DataReader reader)
		{
			Value = reader.GetFloat();
		}

		public void Dispose()
		{
		}

		public void PopulateWithDefaultValues()
		{
			Value = 0.9f;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(Value);
		}
	}
}
