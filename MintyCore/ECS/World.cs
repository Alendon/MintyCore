using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    public class World : IDisposable
    {
        public SystemManager SystemManager { get; private set; }
        public EntityManager EntityManager { get; private set; }

        private readonly GameType _worldGameType;
        public bool IsRenderWorld => (_worldGameType & GameType.Client) != 0;

        public World(GameType gameType = GameType.Local)
        {
            EntityManager = new EntityManager(this);
            SystemManager = new SystemManager(this);
            _worldGameType = gameType;
        }

        public void Tick()
        {
            SystemManager.Execute();
        }

		public void Dispose()
		{
            EntityManager.Dispose();
		}
	}
}