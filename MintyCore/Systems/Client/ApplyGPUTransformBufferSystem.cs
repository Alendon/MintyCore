using Ara3D;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace MintyCore.Systems.Client
{

	[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
	[ExecuteAfter(typeof(IncreaseFrameNumberSystem))]
	class ApplyGPUTransformBufferSystem : ARenderSystem
	{
		public override Identification Identification => SystemIDs.ApplyGPUTransformBuffer;

		ComponentQuery renderableTransformQuery = new();

		private int entityCapacity = _initialTransformCount;
		private int entityCount = 0;
		private bool bufferNeedResize = false;


		public override void Dispose()
		{
			EntityManager.PostEntityCreateEvent -= OnEntityCreate;
			EntityManager.PreEntityDeleteEvent -= OnEntityDelete;

			_entityIndexes.Remove(World);
			_entityPerIndex.Remove(World);


			_transformBuffer[World].resourceSet.Dispose();
			_transformBuffer[World].buffer.Dispose();

			_transformBuffer.Remove(World);
		}

		public override void Execute()
		{
			bool writeAll = false;
			if (bufferNeedResize)
			{
				bufferNeedResize = false;

				var bufferSet = _transformBuffer[World];
				var oldBuffer = bufferSet.buffer;
				var oldResourceSet = bufferSet.resourceSet;

				var newBuffer = VulkanEngine.CreateBuffer<Matrix4x4>(BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, entityCapacity);
				var resourceSetDesc = new ResourceSetDescription(ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Transform), newBuffer);
				var newResourceSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref resourceSetDesc);

				writeAll = true;

				oldBuffer.Dispose();
				oldResourceSet.Dispose();

				_transformBuffer[World] = (newBuffer, newResourceSet);
			}

			var mappedBuffer = VulkanEngine.GraphicsDevice.Map<Matrix4x4>(_transformBuffer[World].buffer, MapMode.Write);

			Dictionary<Entity, int> entityIndexes = _entityIndexes[World];

			foreach (var item in renderableTransformQuery)
			{
				var entity = item.Entity;
				var index = entityIndexes[entity];

				var transform = item.GetReadOnlyComponent<Transform>(ComponentIDs.Transform);
				if (transform.Dirty == 0 && !writeAll) continue;

				mappedBuffer[index] = transform.Value;
			}

			VulkanEngine.GraphicsDevice.Unmap(mappedBuffer.MappedResource.Resource);
		}

		public override void Setup()
		{
			EntityManager.PostEntityCreateEvent += OnEntityCreate;
			EntityManager.PreEntityDeleteEvent += OnEntityDelete;

			_entityIndexes.Add(World, new Dictionary<Entity, int>(entityCapacity));
			_entityPerIndex.Add(World, new Entity[entityCapacity]);

			var buffer = VulkanEngine.CreateBuffer<Matrix4x4>(BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, entityCapacity);
			ResourceSetDescription setDescription = new ResourceSetDescription(ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Transform), buffer);
			var resourceSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref setDescription);
			_transformBuffer.Add(World, (buffer, resourceSet));

			renderableTransformQuery.WithReadOnlyComponents(ComponentIDs.Renderable, ComponentIDs.Transform);
			renderableTransformQuery.Setup(this);
		}


		private void OnEntityDelete(World world, Entity entity)
		{
			if (world != World) return;
			var archetypeContainer = ArchetypeManager.GetArchetype(entity.ArchetypeID);
			if (!archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Renderable) || !archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Transform))
			{
				return;
			}

			int entityIndex = _entityIndexes[World][entity];
			_entityIndexes[World].Remove(entity);
			_entityPerIndex[World][entityIndex] = default;
			entityCount--;
			lastFreeIndex = lastFreeIndex < entityIndex ? lastFreeIndex : entityIndex;
			CheckSize();
		}

		private void OnEntityCreate(World world, Entity entity)
		{
			if (world != World) return;
			var archetypeContainer = ArchetypeManager.GetArchetype(entity.ArchetypeID);
			if (!archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Renderable) || !archetypeContainer.ArchetypeComponents.Contains(ComponentIDs.Transform))
			{
				return;
			}

			entityCount++;
			CheckSize();

			int entityIndex = FindFreeIndex();
			_entityIndexes[World].Add(entity, entityIndex);
			_entityPerIndex[World][entityIndex] = entity;
		}

		private void CheckSize()
		{
			int oldSize = entityCapacity;
			int newSize = entityCapacity;
			if (entityCount == entityCapacity)
			{
				newSize *= 2;
			}
			if (entityCount < entityCapacity / 4 && entityCapacity > _initialTransformCount)
			{
				newSize /= 2;
			}
			if (newSize == oldSize) return;

			CompactData();
			lastFreeIndex = entityCount;

			var newEntityPerIndexes = new Entity[newSize];
			var oldEntityPerIndexes = _entityPerIndex[World];
			Array.Copy(oldEntityPerIndexes, newEntityPerIndexes, entityCount);
			_entityPerIndex[World] = newEntityPerIndexes;

			entityCapacity = newSize;

			bufferNeedResize = true;

		}

		private void CompactData()
		{
			var entityArray = _entityPerIndex[World];
			var entityDic = _entityIndexes[World];

			int freeIndex = -1;
			int takenIndex = entityCapacity;

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
				} while (freeIndex < entityCapacity && entityArray[freeIndex] != default);
			}

			void PreviousTakenIndex()
			{
				do
				{
					takenIndex--;
				} while (takenIndex >= 0 && entityArray[takenIndex] == default);
			}
		}

		private int lastFreeIndex = -1;
		private int FindFreeIndex()
		{
			var entityArray = _entityPerIndex[World];
			do
			{
				lastFreeIndex++;
			} while (lastFreeIndex < entityArray.Length && entityArray[lastFreeIndex] != default);

			if (lastFreeIndex < entityArray.Length && entityArray[lastFreeIndex] == default) return lastFreeIndex;
			lastFreeIndex = 0;

			do
			{
				lastFreeIndex++;
			} while (lastFreeIndex < entityArray.Length && entityArray[lastFreeIndex] != default);

			return entityArray[lastFreeIndex] == default ? lastFreeIndex : throw new Exception("Unexpected behaviour");
		}
	}
}
