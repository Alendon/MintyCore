using System;
using MintyCore.Utils;

namespace MintyCore.Render;

public interface IRenderOutputManager : IDisposable
{
    public void AddRenderOutput<TRenderOutput>(Identification renderOutputId) where TRenderOutput : class;
    public void RemoveRenderOutput(Identification renderOutputId);
    
    public Type GetRenderOutputType(Identification renderOutputId);
}