using System;
using MintyCore.Utils;

namespace MintyCore.Render;

public interface IRenderWorker
{
    void Start();
    void Stop();
    bool IsRunning();

    void SetInputDependencyNew<TRenderInput>(Identification renderModuleId, Identification inputId,
        Action<TRenderInput> callback)
        where TRenderInput : class, IRenderInput;

    void SetOutputDependencyNew<TModuleOutput>(Identification renderModuleId, Identification outputId,
        Action<TModuleOutput> callback) where TModuleOutput : class;

    void SetOutputProviderNew<TModuleOutput>(Identification renderModuleId, Identification outputId,
        Func<TModuleOutput> outputGetter) where TModuleOutput : class;
}