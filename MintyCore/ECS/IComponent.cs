using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	public interface IComponent : IDisposable
	{
		//Byte instead of bool as a bool is not blittable
		byte Dirty { get; set; }


		void PopulateWithDefaultValues();
		Identification Identification { get; }
		void Serialize( DataWriter writer );
		void Deserialize( DataReader reader );
	}
}
