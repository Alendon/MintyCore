using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public interface IRenderModuleDataAccessor
{
    Func<TIntermediateData?> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
        RenderModule inputModule) where TIntermediateData : IntermediateData;
}