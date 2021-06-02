using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.ECS
{
	public class World
	{
		public SystemManager SystemManager { get; private set; }
		public EntityManager EntityManager { get; private set; }

		public World()
		{
			EntityManager = new EntityManager();
			SystemManager = new SystemManager( this );
		}

		public void Tick()
		{
			SystemManager.Execute();
		}
	}
}
