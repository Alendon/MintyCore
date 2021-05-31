using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	public interface IComponent : IDisposable
	{
		//Byte instead of bool because a bool is not blittable
		byte Dirty { get; }
		void PopulateWithDefaultValues();
		Identification Identification { get; }
		void Serialize( DataWriter writer );
		void Deserialize( DataReader reader );
	}
}
