using System;
using System.Collections.Generic;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

public abstract class InputModule : IDisposable
{
    public abstract void Setup();
    public abstract void Update(CommandBuffer commandBuffer);
    public abstract Identification Identification { get; }
    
    /// <inheritdoc />
    public abstract void Dispose();
}