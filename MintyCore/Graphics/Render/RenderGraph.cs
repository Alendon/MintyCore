using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Render;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

internal class RenderGraph(
    IInputDataManager inputDataManager,
    IInputModuleManager inputModuleManager,
    IIntermediateDataManager intermediateDataManager,
    IAsyncFenceAwaiter fenceAwaiter,
    IVulkanEngine vulkanEngine,
    IAllocationHandler allocationHandler)
{
    private IContainer? _inputModuleContainer;
    private ModuleDataAccessor? _moduleDataAccessor;

    private IReadOnlyList<InputModuleReference>? _sortedInputModules;
    private readonly HashSet<Identification> _updatedInputData = new();
    private readonly HashSet<Identification> _updatedIntermediateData = new();

    private CommandPool _inputModuleCommandPool;
    private CommandBuffer _inputModuleCommandBuffer;

    private volatile bool _isRunning;
    private Thread? _thread;

    public void Start()
    {
        Setup();
        _isRunning = true;

        _thread = new Thread(Work);
        _thread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        VulkanUtils.Assert(vulkanEngine.Vk.DeviceWaitIdle(vulkanEngine.Device));

        _thread?.Join();
        _thread = null;

        _inputModuleContainer?.Dispose();
        _inputModuleContainer = null;

        _moduleDataAccessor?.Clear();
        _moduleDataAccessor = null;

        _sortedInputModules = null;
        _updatedInputData.Clear();
        _updatedIntermediateData.Clear();

        DestroyInputCommandBuffer();
    }


    private void Work()
    {
        if (_moduleDataAccessor is null)
            throw new MintyCoreException("Render graph not setup");

        while (_isRunning)
        {
            //TODO add timer to update frame time and optionally wait to limit fps

            var inputTask = BeginProcessingInputModules();
            var renderTask = BeginProcessingRenderModules();


            Task.WaitAll(inputTask, renderTask);

            var inputFence = SubmitInputWork();
            SubmitRenderWork();

            fenceAwaiter.AwaitAsync(inputFence).Wait();

            inputFence.Dispose();

            _moduleDataAccessor.UpdateIntermediateData();
        }
    }

    private void SubmitRenderWork()
    {
        throw new NotImplementedException();
    }

    private Task BeginProcessingRenderModules()
    {
        throw new NotImplementedException();
    }

    private void Setup()
    {
        AllocateInputCommandBuffer();

        _moduleDataAccessor = new ModuleDataAccessor(inputDataManager, intermediateDataManager);

        var inputModules = inputModuleManager.CreateInputModuleInstances(out _inputModuleContainer);

        foreach (var inputModule in inputModules.Values)
        {
            inputModule.ModuleDataAccessor = _moduleDataAccessor;
            inputModule.Setup();
        }

        //TODO Setup render modules

        _moduleDataAccessor.ValidateIntermediateDataProvided();

        var sortedInputModules = _moduleDataAccessor.SortInputModules(inputModules.Keys);

        _sortedInputModules = sortedInputModules.Select(id => new InputModuleReference
        {
            Module = inputModules[id],
            InputDataIds = _moduleDataAccessor.GetInputModuleConsumedInputDataIds(id).ToList(),
            ConsumedIntermediateIds = _moduleDataAccessor.GetInputModuleConsumedIntermediateDataIds(id).ToList(),
            ProvidedIntermediateIds = _moduleDataAccessor.GetInputModuleProvidedIntermediateDataIds(id).ToList()
        }).ToList();
    }

    private Task BeginProcessingInputModules()
    {
        _updatedInputData.Clear();
        _updatedIntermediateData.Clear();

        PrepareInputCommandBuffer();

        _updatedInputData.UnionWith(inputDataManager.GetUpdatedInputDataIds(reset: true));

        return Task.Run(ProcessInputModules);
    }

    private unsafe ManagedFence SubmitInputWork()
    {
        var inputModuleCommandBuffer = _inputModuleCommandBuffer;

        VulkanUtils.Assert(vulkanEngine.Vk.EndCommandBuffer(inputModuleCommandBuffer));

        var fence = new ManagedFence(vulkanEngine, allocationHandler);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &inputModuleCommandBuffer
        };

        VulkanUtils.Assert(vulkanEngine.Vk.QueueSubmit(vulkanEngine.GraphicQueue.queue, 1, &submitInfo,
            fence.InternalFence));

        return fence;
    }

    private void ProcessInputModules()
    {
        if (_sortedInputModules is null || _moduleDataAccessor is null)
        {
            throw new MintyCoreException("Render graph not setup");
        }

        foreach (var inputModuleReference in _sortedInputModules)
        {
            var consumedInputData = inputModuleReference.InputDataIds;
            var consumedIntermediateData = inputModuleReference.ConsumedIntermediateIds;
            var providedIntermediateData = inputModuleReference.ProvidedIntermediateIds;

            //check if at least one input or intermediate data is updated
            if (!consumedInputData.Any(_updatedInputData.Contains) &&
                !consumedIntermediateData.Any(_updatedIntermediateData.Contains))
                continue;
            
            inputModuleReference.Module.Update(_inputModuleCommandBuffer);
            
            _updatedIntermediateData.UnionWith(providedIntermediateData);
        }
    }

    private unsafe void AllocateInputCommandBuffer()
    {
        var commandPoolCreateInfo = new CommandPoolCreateInfo()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = vulkanEngine.GraphicQueue.familyIndex
        };
        
        VulkanUtils.Assert(vulkanEngine.Vk.CreateCommandPool(vulkanEngine.Device, commandPoolCreateInfo, null, out _inputModuleCommandPool));
        
        var commandBufferAllocateInfo = new CommandBufferAllocateInfo()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _inputModuleCommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        
        VulkanUtils.Assert(vulkanEngine.Vk.AllocateCommandBuffers(vulkanEngine.Device, commandBufferAllocateInfo, out _inputModuleCommandBuffer));
    }

    private void PrepareInputCommandBuffer()
    {
        var commandBufferBeginInfo = new CommandBufferBeginInfo()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        
        VulkanUtils.Assert(vulkanEngine.Vk.BeginCommandBuffer(_inputModuleCommandBuffer, commandBufferBeginInfo));
    }

    private unsafe void DestroyInputCommandBuffer()
    {
        vulkanEngine.Vk.DestroyCommandPool(vulkanEngine.Device, _inputModuleCommandPool, null);
        
        _inputModuleCommandBuffer = default;
        _inputModuleCommandPool = default;
    }

    private record struct InputModuleReference(
        InputModule Module,
        IReadOnlyList<Identification> InputDataIds,
        IReadOnlyList<Identification> ConsumedIntermediateIds,
        IReadOnlyList<Identification> ProvidedIntermediateIds);
}