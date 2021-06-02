using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Registries
{
	public interface IRegistry
	{
		void PreRegister();

		void Register();

		void PostRegister();

		void Clear();

		ushort RegistryID { get; }

		ICollection<ushort> RequiredRegistries { get; }
	}
}
