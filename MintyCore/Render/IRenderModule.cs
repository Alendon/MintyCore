using System;
using MintyCore.Render.VulkanObjects;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

public interface IRenderModule : IDisposable
{
    void Process(ManagedCommandBuffer cb);
    void Initialize(IRenderWorker renderWorker);
}