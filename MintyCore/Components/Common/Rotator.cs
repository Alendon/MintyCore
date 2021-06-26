using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Components.Common
{
	struct Rotator : IComponent
	{
		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Rotator;

		public float xSpeed;
		public float ySpeed;
		public float zSpeed;

		public void Deserialize(DataReader reader)
		{
			xSpeed = reader.GetFloat();
			ySpeed = reader.GetFloat();
			zSpeed = reader.GetFloat();
		}

		public void Dispose()
		{
		}

		public void PopulateWithDefaultValues()
		{
			xSpeed = 0.001f;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(xSpeed);
			writer.Put(ySpeed);
			writer.Put(zSpeed);
		}
	}
}
