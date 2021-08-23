﻿using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Components.Common
{
	public struct Rotator : IComponent
	{
		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Rotator;

		public Vector3 Speed;


		public void Deserialize(DataReader reader)
		{
			Speed = reader.GetVector3();
		}


		public void PopulateWithDefaultValues()
		{
			Speed = new Vector3(0.1f, 0, 0);
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(Speed);
		}

		public void IncreaseRefCount()
		{
		}

		public void DecreaseRefCount()
		{
		}
	}
}
