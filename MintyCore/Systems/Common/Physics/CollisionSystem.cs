using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BepuUtilities;
using BepuUtilities.Memory;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics
{
    /// <summary>
    ///     System which adds and removes collision object to the <see cref="World.PhysicsWorld" /> and updates the associated
    ///     <see cref="Entity" />
    /// </summary>
    [ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
    public partial class CollisionSystem : ASystem
    {
        private readonly Stopwatch _physic = new();
        private double passedDeltaTime = 0;
        
        [ComponentQuery] private readonly CollisionApplyQuery<(Position, Rotation ), Collider> _query = new();

        /// <summary>
        ///     <see cref="Identification" /> of the <see cref="CollisionSystem" />
        /// </summary>
        public override Identification Identification => SystemIDs.Collision;

        /// <inheritdoc />
        public override void Setup()
        {
            _query.Setup(this);

            _physic.Start();
            EntityManager.PreEntityDeleteEvent += OnEntityDelete;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
        }

        /// <summary>
        ///     Checks if the entity has a rigid body in the physics world and removes it
        /// </summary>
        private void OnEntityDelete(World world, Entity entity)
        {
            if (World != world || !ArchetypeManager.HasComponent(entity.ArchetypeId, ComponentIDs.Collider)) return;
            
            var collider = World.EntityManager.GetComponent<Collider>(entity);

            var bodyRef = World.PhysicsWorld.Simulation.Bodies.GetBodyReference(collider.BodyHandle);
            if(!bodyRef.Exists) return;
            
            World.PhysicsWorld.Simulation.Bodies.Remove(collider.BodyHandle);
        }

        public override void PreExecuteMainThread()
        {
            if(World is null) return;
        }

        private TaskDispatcher _dispatcher = new TaskDispatcher();
        
        /// <inheritdoc />
        protected override void Execute()
        {
            if(World is null) return;
            
            _physic.Stop();
            passedDeltaTime += _physic.Elapsed.TotalSeconds;
            while (passedDeltaTime >= PhysicsWorld.FixedDeltaTime)
            {
                World.PhysicsWorld.StepSimulation(PhysicsWorld.FixedDeltaTime/*, _dispatcher*/);
                passedDeltaTime -= PhysicsWorld.FixedDeltaTime;
            }
            _physic.Restart();

            foreach (var entity in _query)
            {
                var collider = entity.GetCollider();

                var bodyRef = World.PhysicsWorld.Simulation.Bodies.GetBodyReference(collider.BodyHandle);
                
                if(!bodyRef.Exists) continue;
                
                ref var rot = ref entity.GetRotation();
                ref var pos = ref entity.GetPosition();

                {
                    rot.Value = bodyRef.Pose.Orientation;
                    pos.Value = bodyRef.Pose.Position;

                    //pos.Dirty = 1;
                    //rot.Dirty = 1;
                }
            }
        }

        private class TaskDispatcher : IThreadDispatcher
        {
            private BufferPool[] _bufferPools;
            
            public TaskDispatcher()
            {
                _bufferPools = new BufferPool[ThreadCount];
                for (int i = 0; i < ThreadCount; i++)
                {
                    _bufferPools[i] = new BufferPool();
                }
            }
            
            public void DispatchWorkers(Action<int> workerBody)
            {
                Task[] tasks = new Task[ThreadCount];

                for (int i = 0; i < tasks.Length; i++)
                {
                    var i1 = i;
                    tasks[i] = Task.Run(()=> workerBody(i1));
                }

                Task.WaitAll(tasks);
            }

            public BufferPool GetThreadMemoryPool(int workerIndex)
            {
                return _bufferPools[workerIndex];
            }

            public int ThreadCount => Environment.ProcessorCount;
        }
    }
}