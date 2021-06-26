using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    public class World
    {
        public SystemManager SystemManager { get; private set; }
        public EntityManager EntityManager { get; private set; }

        private GameType _worldGameType;
        public bool IsRenderWorld => (_worldGameType & GameType.Client) != 0;

        public World()
        {
            EntityManager = new EntityManager(this);
            SystemManager = new SystemManager(this);
        }

        public void Tick()
        {
            SystemManager.Execute();
        }
    }
}