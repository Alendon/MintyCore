using System;
using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Render;

[Singleton<IRenderOutputManager>(SingletonContextFlags.NoHeadless)]
internal sealed class RenderOutputManager : IRenderOutputManager
{
    private readonly Dictionary<Identification, Type> _outputTypes = new();

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void AddRenderOutput<TRenderOutput>(Identification renderOutputId) where TRenderOutput : class
    {
        _outputTypes.Add(renderOutputId, typeof(TRenderOutput));
    }

    /// <inheritdoc />
    public void RemoveRenderOutput(Identification renderOutputId)
    {
        _outputTypes.Remove(renderOutputId);
    }

    /// <inheritdoc />
    public Type GetRenderOutputType(Identification renderOutputId)
    {
        return _outputTypes[renderOutputId];
    }
}