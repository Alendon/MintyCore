using System;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

public interface IRenderModule : IDisposable
{
    void Process(CommandBuffer cb);
    void Initialize(IRenderWorker renderWorker);
    
    RenderPassBeginInfoWrapper GetRenderPassBeginInfo();
}