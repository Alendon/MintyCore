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
using Silk.NET.Vulkan;
using Buffer = System.Buffer;

namespace MintyCore.Systems.Client
{
    [ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
    [ExecuteAfter(typeof(ApplyGpuCameraBufferSystem))]
    [ExecutionSide(GameType.CLIENT)]
    public unsafe partial class RenderInstancedSystem : ASystem
    {
        [ComponentQuery] private ComponentQuery<object, (InstancedRenderAble, Transform)> _componentQuery = new();
        [ComponentQuery] private CameraComponentQuery<object, Camera> _cameraComponentQuery = new();

        private const int InitialSize = 512;

        public override Identification Identification => SystemIDs.RenderInstanced;

        private Dictionary<Identification, (MemoryBuffer buffer, IntPtr mappedData, int capacity, int currentIndex)>
            _stagingBuffers = new();

        private Dictionary<Identification, MemoryBuffer[]> _instanceBuffers = new();
        private Dictionary<Identification, uint> _drawCount = new();

        private CommandPool _bufferCommandPool;
        private CommandPool[] _drawCommandPools;

        private Queue<Fence> _availableFences = new();
        private Fence[] _waitFences = new Fence[16];

        private CommandBuffer buffer;

        protected override void Execute()
        {
            //Iterate over each entity and write the current transform to the corresponding staging buffer
            _drawCount.Clear();
            foreach (var entity in _componentQuery)
            {
                var renderAble = entity.GetInstancedRenderAble();
                var transform = entity.GetTransform();

                if (!World!.IsServerWorld)
                {
                    Matrix4x4.Decompose(transform.Value, out var scale, out var rot, out var trans);
                }
                
                if(renderAble.MaterialMeshCombination == Identification.Invalid) continue;

                WriteToBuffer(renderAble.MaterialMeshCombination, transform.Value);

                if (!_drawCount.ContainsKey(renderAble.MaterialMeshCombination))
                    _drawCount.Add(renderAble.MaterialMeshCombination, 0);
                _drawCount[renderAble.MaterialMeshCombination] += 1;
            }

            //submit the staging buffers and write to each instance buffer
            SubmitBuffers();
            
            VulkanUtils.Assert(VulkanEngine.Vk.ResetCommandPool( VulkanEngine.Device,_drawCommandPools[VulkanEngine._imageIndex], 0));

            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Secondary,
                PNext = null,
                CommandPool = _drawCommandPools[VulkanEngine._imageIndex],
                CommandBufferCount = 1
            };
            VulkanUtils.Assert(VulkanEngine.Vk.AllocateCommandBuffers(VulkanEngine.Device, allocateInfo, out buffer));

            CommandBufferInheritanceInfo inheritanceInfo = new()
            {
                SType = StructureType.CommandBufferInheritanceInfo,
                Framebuffer = default,
                Subpass = 0,
                PipelineStatistics = default,
                PNext = null,
                RenderPass = RenderPassHandler.MainRenderPass,
                QueryFlags = 0,
                OcclusionQueryEnable = Vk.False
            };

            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                PNext = null,
                Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit |
                        CommandBufferUsageFlags.CommandBufferUsageRenderPassContinueBit,
                PInheritanceInfo = &inheritanceInfo
            };

            VulkanUtils.Assert(VulkanEngine.Vk.BeginCommandBuffer(buffer, beginInfo));

            foreach (var cameraEntity in _cameraComponentQuery)
            {
                var camera = cameraEntity.GetCamera();
                foreach (var (id, drawCount) in _drawCount)
                {
                    (Mesh mesh, Material[] material) = InstancedRenderDataHandler.GetMeshMaterial(id);
                    MemoryBuffer instanceBuffer = _instanceBuffers[id][VulkanEngine._imageIndex];

                    for (int i = 0; i < mesh.SubMeshIndexes.Length; i++)
                    {
                        var (startIndex, length) = mesh.SubMeshIndexes[i];
                        
                        material[i].Bind(buffer);
                        
                        VulkanEngine.Vk.CmdBindDescriptorSets(buffer, PipelineBindPoint.Graphics, material[i].PipelineLayout,
                            0, camera.GpuTransformDescriptors.AsSpan().Slice((int)VulkanEngine._imageIndex, 1), 0, null );
                    
                        VulkanEngine.Vk.CmdBindVertexBuffers(buffer, 0,  1, mesh.MemoryBuffer.Buffer, 0);
                        VulkanEngine.Vk.CmdBindVertexBuffers(buffer, 1,  1, instanceBuffer.Buffer, 0);
                    
                    
                        VulkanEngine.Vk.CmdDraw(buffer, length, drawCount, startIndex,0);
                    }
                }
            }
            

            VulkanEngine.Vk.EndCommandBuffer(buffer);
        }

        public override void PostExecuteMainThread()
        {
            VulkanEngine.ExecuteSecondary(buffer);
        }

        private void SubmitBuffers()
        {
            uint[] queueFamilies = { VulkanEngine.QueueFamilyIndexes.PresentFamily!.Value };

            int bufferCount = _stagingBuffers.Count;
            if (_waitFences.Length < bufferCount)
            {
                int newSize = _waitFences.Length;
                while (newSize < bufferCount) newSize *= 2;
                _waitFences = new Fence[newSize];
            }

            int submissionIndex = 0;
            foreach (var (id, (buffer, _, capacity, index)) in _stagingBuffers)
            {
                buffer.UnMap();

                MemoryBuffer instanceBuffer;
                if (!_instanceBuffers.ContainsKey(id))
                {
                    _instanceBuffers.Add(id, new MemoryBuffer[VulkanEngine.SwapchainImageCount]);
                    for (int i = 0; i < VulkanEngine.SwapchainImageCount; i++)
                    {
                        instanceBuffer = MemoryBuffer.Create(
                            BufferUsageFlags.BufferUsageTransferDstBit | BufferUsageFlags.BufferUsageVertexBufferBit,
                            buffer.Size, SharingMode.Exclusive, queueFamilies.AsSpan(),
                            MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
                        _instanceBuffers[id][i] = instanceBuffer;
                    }
                }

                instanceBuffer = _instanceBuffers[id][VulkanEngine._imageIndex];
                if (instanceBuffer.Size < buffer.Size)
                {
                    instanceBuffer.Dispose();

                    instanceBuffer = MemoryBuffer.Create(
                        BufferUsageFlags.BufferUsageTransferDstBit | BufferUsageFlags.BufferUsageVertexBufferBit,
                        buffer.Size, SharingMode.Exclusive, queueFamilies.AsSpan(),
                        MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
                    _instanceBuffers[id][VulkanEngine._imageIndex] = instanceBuffer;
                }

                _stagingBuffers[id] = (buffer, IntPtr.Zero, capacity, 0);

                CommandBufferAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    Level = CommandBufferLevel.Primary,
                    CommandPool = _bufferCommandPool,
                    PNext = null,
                    CommandBufferCount = 1
                };
                CommandBuffer submitBuffer;
                VulkanUtils.Assert(
                    VulkanEngine.Vk.AllocateCommandBuffers(VulkanEngine.Device, allocateInfo, out submitBuffer));

                CommandBufferBeginInfo beginInfo = new()
                {
                    SType = StructureType.CommandBufferBeginInfo,
                    PNext = null,
                    Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit,
                    PInheritanceInfo = null
                };
                VulkanUtils.Assert(VulkanEngine.Vk.BeginCommandBuffer(submitBuffer, beginInfo ));

                
                BufferCopy bufferCopy = new()
                {
                    Size = (ulong)(index * sizeof(Matrix4x4)),
                    DstOffset = 0,
                    SrcOffset = 0
                };
                VulkanEngine.Vk.CmdCopyBuffer(submitBuffer, buffer.Buffer, instanceBuffer.Buffer, 1, bufferCopy);
                SubmitInfo submitInfo = new()
                {
                    SType = StructureType.SubmitInfo,
                    PNext = null,
                    CommandBufferCount = 1,
                    PCommandBuffers = &submitBuffer,
                    PSignalSemaphores = null,
                    PWaitSemaphores = null,
                    SignalSemaphoreCount = 0,
                    WaitSemaphoreCount = 0,
                    PWaitDstStageMask = null
                };

                VulkanUtils.Assert(VulkanEngine.Vk.EndCommandBuffer(submitBuffer));
                
                var fence = GetFence();
                VulkanUtils.Assert(VulkanEngine.Vk.QueueSubmit(VulkanEngine.PresentQueue, 1, submitInfo, fence));
                _waitFences[submissionIndex] = fence;
                submissionIndex++;
            }

            if (submissionIndex > 0)
            {
                VulkanUtils.Assert(VulkanEngine.Vk.WaitForFences(VulkanEngine.Device,
                    _waitFences.AsSpan(0, submissionIndex), Vk.True, ulong.MaxValue));
                VulkanUtils.Assert(VulkanEngine.Vk.ResetFences(VulkanEngine.Device, _waitFences.AsSpan(0, submissionIndex)));
                VulkanUtils.Assert(VulkanEngine.Vk.ResetCommandPool(VulkanEngine.Device, _bufferCommandPool, 0));
            }

            for (int i = 0; i < submissionIndex; i++)
            {
                ReturnFence(_waitFences[i]);
            }
        }

        private Fence GetFence()
        {
            Fence fence;
            if (_availableFences.TryDequeue(out fence))
            {
                return fence;
            }

            FenceCreateInfo createInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                PNext = null,
                Flags = 0
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateFence(VulkanEngine.Device, createInfo,
                VulkanEngine.AllocationCallback, out fence));
            return fence;
        }

        private void ReturnFence(Fence fence)
        {
            _availableFences.Enqueue(fence);
        }

        private void WriteToBuffer(Identification materialMesh, in Matrix4x4 transformData)
        {
            uint[] queueFamilies = { VulkanEngine.QueueFamilyIndexes.PresentFamily!.Value };
            if (!_stagingBuffers.ContainsKey(materialMesh))
            {
                var memoryBuffer = MemoryBuffer.Create(BufferUsageFlags.BufferUsageTransferSrcBit,
                    (ulong)(sizeof(Matrix4x4) * InitialSize), SharingMode.Exclusive, queueFamilies.AsSpan(),
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit);

                _stagingBuffers.Add(materialMesh, (memoryBuffer, (IntPtr)memoryBuffer.MapMemory(), InitialSize, 0));
            }

            var (buffer, data, capacity, index) = _stagingBuffers[materialMesh];

            if (data == IntPtr.Zero)
            {
                data = (IntPtr)buffer.MapMemory();
            }

            if (capacity <= index)
            {
                var memoryBuffer = MemoryBuffer.Create(BufferUsageFlags.BufferUsageTransferSrcBit,
                    (ulong)(sizeof(Matrix4x4) * capacity * 2), SharingMode.Exclusive, queueFamilies.AsSpan(),
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit);

                Transform* oldData = (Transform*)data;
                Transform* newData = (Transform*)memoryBuffer.MapMemory();

                Buffer.MemoryCopy(oldData, newData, sizeof(Matrix4x4) * capacity * 2, sizeof(Matrix4x4) * capacity);

                buffer.UnMap();
                buffer.Dispose();

                buffer = memoryBuffer;
                data = (IntPtr)newData;
                capacity *= 2;
            }

            // ReSharper disable once PossibleNullReferenceException
            ((Matrix4x4*)data)[index] = transformData;

            _stagingBuffers[materialMesh] = (buffer, data, capacity, index + 1);
        }

        public override void Setup()
        {
            _componentQuery.Setup(this);
            _cameraComponentQuery.Setup(this);

            CommandPoolCreateInfo createInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                PNext = null,
                QueueFamilyIndex = VulkanEngine.QueueFamilyIndexes.PresentFamily!.Value,
                Flags = 0
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateCommandPool(VulkanEngine.Device, createInfo,
                VulkanEngine.AllocationCallback, out _bufferCommandPool));

            _drawCommandPools = new CommandPool[VulkanEngine.SwapchainImageCount];
            createInfo.QueueFamilyIndex = VulkanEngine.QueueFamilyIndexes.GraphicsFamily!.Value;
            for (int i = 0; i < VulkanEngine.SwapchainImageCount; i++)
            {
                VulkanUtils.Assert(VulkanEngine.Vk.CreateCommandPool(VulkanEngine.Device, createInfo,
                    VulkanEngine.AllocationCallback, out _drawCommandPools[i]));
            }
        }

        public override void Dispose()
        {
            foreach (var (_, memoryBuffers) in _instanceBuffers)
            {
                foreach (var memoryBuffer in memoryBuffers)
                {
                    memoryBuffer.Dispose();
                }
            }

            foreach (var (_, stagingBuffer) in _stagingBuffers)
            {
                stagingBuffer.buffer.Dispose();
            }
            
            VulkanEngine.Vk.DestroyCommandPool(VulkanEngine.Device, _bufferCommandPool,
                VulkanEngine.AllocationCallback);
            foreach (var commandPool in _drawCommandPools)
            {
                VulkanEngine.Vk.DestroyCommandPool(VulkanEngine.Device, commandPool, VulkanEngine.AllocationCallback);
            }

            while (_availableFences.TryDequeue(out var fence))
            {
                VulkanEngine.Vk.DestroyFence(VulkanEngine.Device, fence, VulkanEngine.AllocationCallback);
            }
        }
    }
}