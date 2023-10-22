using MintyCore.Utils;

namespace MintyCore.Render;

public interface IRenderManager
{
    void AddRenderModule<TRenderModule>(Identification renderModuleId) where TRenderModule : IRenderModule;
    void RemoveRenderModule(Identification renderModuleId);
    
    void SetRenderModuleActive(Identification renderModuleId, bool active);
    bool IsRenderModuleActive(Identification renderModuleId);
}