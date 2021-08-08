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
	struct Spring : IComponent
	{
		public Vector3 WorldPoint;
		public Vector3 LocalPoint;
		public float SpringConstant;
		public float SpringLength;

		public byte Dirty {  get; set; }

		public Identification Identification => ComponentIDs.Spring;

		public void Deserialize(DataReader reader)
		{
			WorldPoint = reader.GetVector3();
			LocalPoint = reader.GetVector3();
			SpringConstant = reader.GetFloat();
			SpringLength = reader.GetFloat();
		}

		public void Dispose()
		{
		}

		public void PopulateWithDefaultValues()
		{
			SpringLength = 1;
			SpringConstant = 1;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(WorldPoint);
			writer.Put(LocalPoint);
			writer.Put(SpringConstant);
			writer.Put(SpringLength);
		}
	}
}
