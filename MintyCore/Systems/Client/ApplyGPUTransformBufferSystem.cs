using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Client
{
    [ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
    [ExecuteAfter(typeof(IncreaseFrameNumberSystem))]
    internal partial class ApplyGpuTransformBufferSystem : ARenderSystem
    {
        private bool _bufferNeedResize;

        private int _entityCapacity = InitialTransformCount;
        private int _entityCount;

        private int _lastFreeIndex = -1;

        [ComponentQuery] private readonly Query<object, (RenderAble, Transform)> _renderableTransformQuery = new();

        public override Identification Identification => SystemIDs.ApplyGpuTransformBuffer;


        public override void Dispose()
        {
            /*EntityManager.PostEntityCreateEvent -= OnEntityCreate;
            EntityManager.PreEntityDeleteEvent -= OnEntityDelete;

            EntityIndexes.Remove(World);
            EntityPerIndex.Remove(World);


            TransformBuffer[World].resourceSet.Dispose();
            TransformBuffer[World].buffer.Dispose();

            TransformBuffer.Remove(World);*/
        }

        protected override void Execute()
        {
           /* var writeAll = false;
            if (_bufferNeedResize)
            {
                _bufferNeedResize = false;

                var (oldBuffer, oldResourceSet) = TransformBuffer[World];

                var newBuffer =
                    VulkanEngine.CreateBuffer<Matrix4x4>(BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic,
                        _entityCapacity);
                var resourceSetDesc =
                    new ResourceSetDescription(ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Transform),
                        newBuffer);
                var newResourceSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref resourceSetDesc);

                writeAll = true;

                oldBuffer.Dispose();
                oldResourceSet.Dispose();

                TransformBuffer[World] = (newBuffer, newResourceSet);
            }

            var mappedBuffer = VulkanEngine.GraphicsDevice.Map<Matrix4x4>(TransformBuffer[World].buffer, MapMode.Write);

            var entityIndexes = EntityIndexes[World];

            foreach (var item in _renderableTransformQuery)
            {
                var entity = item.Entity;
                var index = entityIndexes[entity];

                var transform = item.GetTransform();
                if (transform.Dirty == 0 && !writeAll) continue;

                mappedBuffer[index] = transform.Value;
            }

            VulkanEngine.GraphicsDevice.Unmap(mappedBuffer.MappedResource.Resource);*/
        }

        public override void Setup()
        {
            /*EntityManager.PostEntityCreateEvent += OnEntityCreate;
            EntityManager.PreEntityDeleteEvent += OnEntityDelete;

            EntityIndexes.Add(World, new Dictionary<Entity, int>(_entityCapacity));
            EntityPerIndex.Add(World, new Entity[_entityCapacity]);

            var buffer =
                VulkanEngine.CreateBuffer<Matrix4x4>(BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic,
                    _entityCapacity);
            var setDescription =
                new ResourceSetDescription(ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Transform),
                    buffer);
            var resourceSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref setDescription);
            TransformBuffer.Add(World, (buffer, resourceSet));

            _renderableTransformQuery.Setup(this);*/
        }


        private void OnEntityDelete(World world, Entity entity)
        {
            if (world != World) return;
            var archetypeContainer = ArchetypeManager.GetArchetype(entity.ArchetypeId);
            if (!archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Renderable) ||
                !archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Transform)) return;

            var entityIndex = EntityIndexes[World][entity];
            EntityIndexes[World].Remove(entity);
            EntityPerIndex[World][entityIndex] = default;
            _entityCount--;
            _lastFreeIndex = _lastFreeIndex < entityIndex ? _lastFreeIndex : entityIndex;
            CheckSize();
        }

        private void OnEntityCreate(World world, Entity entity)
        {
            if (world != World) return;
            var archetypeContainer = ArchetypeManager.GetArchetype(entity.ArchetypeId);
            if (!archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Renderable) ||
                !archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Transform)) return;

            _entityCount++;
            CheckSize();

            var entityIndex = FindFreeIndex();
            EntityIndexes[World].Add(entity, entityIndex);
            EntityPerIndex[World][entityIndex] = entity;
        }

        private void CheckSize()
        {
            var oldSize = _entityCapacity;
            var newSize = _entityCapacity;
            if (_entityCount == _entityCapacity) newSize *= 2;
            if (_entityCount < _entityCapacity / 4 && _entityCapacity > InitialTransformCount) newSize /= 2;
            if (newSize == oldSize) return;

            CompactData();
            _lastFreeIndex = _entityCount;

            var newEntityPerIndexes = new Entity[newSize];
            var oldEntityPerIndexes = EntityPerIndex[World];
            Array.Copy(oldEntityPerIndexes, newEntityPerIndexes, _entityCount);
            EntityPerIndex[World] = newEntityPerIndexes;

            _entityCapacity = newSize;

            _bufferNeedResize = true;
        }

        private void CompactData()
        {
            var entityArray = EntityPerIndex[World];
            var entityDic = EntityIndexes[World];

            var freeIndex = -1;
            var takenIndex = _entityCapacity;

            NextFreeIndex();
            PreviousTakenIndex();

            while (freeIndex < takenIndex)
            {
                var entity = entityArray[takenIndex];
                entityArray[freeIndex] = entity;
                entityArray[takenIndex] = default;

                entityDic[entity] = freeIndex;

                NextFreeIndex();
                PreviousTakenIndex();
            }


            void NextFreeIndex()
            {
                do
                {
                    freeIndex++;
                } while (freeIndex < _entityCapacity && entityArray[freeIndex] != default);
            }

            void PreviousTakenIndex()
            {
                do
                {
                    takenIndex--;
                } while (takenIndex >= 0 && entityArray[takenIndex] == default);
            }
        }

        private int FindFreeIndex()
        {
            var entityArray = EntityPerIndex[World];
            do
            {
                _lastFreeIndex++;
            } while (_lastFreeIndex < entityArray.Length && entityArray[_lastFreeIndex] != default);

            if (_lastFreeIndex < entityArray.Length && entityArray[_lastFreeIndex] == default) return _lastFreeIndex;
            _lastFreeIndex = 0;

            do
            {
                _lastFreeIndex++;
            } while (_lastFreeIndex < entityArray.Length && entityArray[_lastFreeIndex] != default);

            return entityArray[_lastFreeIndex] == default
                ? _lastFreeIndex
                : throw new Exception("Unexpected behaviour");
        }
    }
}