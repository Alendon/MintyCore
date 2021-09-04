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
        public readonly bool IsServerWorld;

        /// <summary>
        ///     Create a new World
        /// </summary>
        /// <param name="gameType"></param>
        public World(bool isServerWorld)
        {
            IsServerWorld = isServerWorld;
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