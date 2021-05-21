using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.ECS
{
	public class World
	{
		public SystemManager SystemManager { get; private set; }
		public EntityManager EntityManager { get; private set; }
	}
}
