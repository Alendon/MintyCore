using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Graphics.RenderGraphTest;

[PublicAPI]
public abstract record RenderModuleDescription(
    RenderResourceAccess[] ResourceAccesses,
    RenderInfo? RenderInfo,
    bool ActiveByDefault,
    Lazy<IReadOnlyDictionary<Identification, (RenderModuleOrdering order, bool moduleMustExist)>>? ordering)
{
    public abstract Type GetRenderModuleType();
}

/// <inheritdoc />
[PublicAPI]
public record RenderModuleDescription<TRenderModule>(
    RenderResourceAccess[] ResourceAccesses,
    RenderInfo? RenderInfo = null,
    bool ActiveByDefault = true,
    Lazy<IReadOnlyDictionary<Identification, (RenderModuleOrdering order, bool moduleMustExist)>>? ordering = null)
    : RenderModuleDescription(ResourceAccesses, RenderInfo, ActiveByDefault, ordering)
{
    /// <inheritdoc />
    public override Type GetRenderModuleType()
    {
        return typeof(TRenderModule);
    }
}

public enum RenderModuleOrdering
{
    Before,
    After
}