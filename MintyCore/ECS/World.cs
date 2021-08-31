using System;
using MintyCore.Physics;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	///     The <see cref="World" /> represents a unique simulation
	/// </summary>
	public class World : IDisposable
    {
        private readonly GameType _worldGameType;

        /// <summary>
        ///     Create a new World
        /// </summary>
        /// <param name="gameType"></param>
        public World(GameType gameType = GameType.LOCAL)
        {
            _worldGameType = gameType;
            EntityManager = new EntityManager(this);
            SystemManager = new SystemManager(this);
            PhysicsWorld = new PhysicsWorld();
        }

        /// <summary>
        ///     The SystemManager of the <see cref="World" />
        /// </summary>
        public SystemManager SystemManager { get; }

        /// <summary>
        ///     The EntityManager of the <see cref="World" />
        /// </summary>
        public EntityManager EntityManager { get; }

        /// <summary>
        ///     The <see cref="PhysicsWorld" /> of the <see cref="World" />
        /// </summary>
        public PhysicsWorld PhysicsWorld { get; }

        /// <summary>
        ///     Get if the <see cref="World" /> gets rendered or not
        /// </summary>
        public bool IsRenderWorld => (_worldGameType & GameType.CLIENT) != 0;

        /// <inheritdoc />
        public void Dispose()
        {
            EntityManager.Dispose();
            PhysicsWorld.Dispose();
        }

        /// <summary>
        ///     Simulate one <see cref="World" /> tick
        /// </summary>
        public void Tick()
        {
            SystemManager.Execute();
        }

        internal void SetupTick()
        {
            SystemManager.ExecuteFinalization();
        }
    }
}