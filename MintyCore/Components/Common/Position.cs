﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common
{
	public struct Position : IComponent
	{
		public byte Dirty { get; set; }

		public Vector3 Value;

		public Identification Identification => ComponentIDs.Position;

		public void Deserialize( DataReader reader )
		{
			Value = reader.GetVector3();
		}
		public void PopulateWithDefaultValues()
		{
			Value = Vector3.Zero;
		}
		public void Serialize( DataWriter writer )
		{
			writer.Put(Value);
		}

		public void IncreaseRefCount()
		{
		}

		public void DecreaseRefCount()
		{
		}
	}
}
