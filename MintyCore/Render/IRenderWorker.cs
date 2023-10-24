using System;
using MintyCore.Utils;

namespace MintyCore.Render;

public interface IRenderWorker
{
    void Start();
    void Stop();
    
    void SetInputDependency(Identification renderModuleId, Identification inputId, Action<object> callback);
    void SetInputDependency<TInputResult>(Identification renderModuleId, Identification inputId, Action<TInputResult> callback);
    
    void SetOutputDependency(Identification renderModuleId, Identification outputId, Action<object> callback);
    void SetOutputDependency<TOutputResult>(Identification renderModuleId, Identification outputId, Action<TOutputResult> callback);
    
    void SetRenderModuleOutput(Identification renderModuleId, Identification outputId, Func<IRenderOutputWrapper> outputGetter);
    void SetRenderModuleOutput<TRenderOutput>(Identification renderModuleId, Identification outputId, Func<IRenderOutputWrapper<TRenderOutput>> outputGetter);
}