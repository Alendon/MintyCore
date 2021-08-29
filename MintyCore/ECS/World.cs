using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Physics;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    /// <summary>
    /// The <see cref="World"/> represents a unique simulation
    /// </summary>
    public class World : IDisposable
    {
        /// <summary>
        /// The SystemManager of the <see cref="World"/>
        /// </summary>
        public SystemManager SystemManager { get; private set; }

        /// <summary>
        /// The EntityManager of the <see cref="World"/>
        /// </summary>
        public EntityManager EntityManager { get; private set; }
        
        public PhysicsWorld PhysicsWorld { get; private set; }

        private readonly GameType _worldGameType;

        /// <summary>
        /// Get if the <see cref="World"/> gets rendered or not
        /// </summary>
        public bool IsRenderWorld => (_worldGameType & GameType.Client) != 0;

        /// <summary>
        /// Create a new World
        /// </summary>
        /// <param name="gameType"></param>
        public World(GameType gameType = GameType.Local)
        {
	        _worldGameType = gameType;
            EntityManager = new EntityManager(this);
            SystemManager = new SystemManager(this);
            PhysicsWorld = new PhysicsWorld(this);
        }

        /// <summary>
        /// Simulate one <see cref="World"/> tick
        /// </summary>
        public void Tick()
        {
            SystemManager.Execute();
        }

        internal void SetupTick()
		{
            SystemManager.ExecuteFinalization();
		}

        /// <inheritdoc/>
		public void Dispose()
		{
            EntityManager.Dispose();
            PhysicsWorld.Dispose();
		}
	}
}