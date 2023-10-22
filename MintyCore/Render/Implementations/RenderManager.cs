using MintyCore.Utils;

namespace MintyCore.Render.Implementations;

[Singleton<IRenderManager>(SingletonContextFlags.NoHeadless)]
internal class RenderManager : IRenderManager
{
    /// <inheritdoc />
    public void AddRenderModule<TRenderModule>(Identification renderModuleId) where TRenderModule : IRenderModule
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void RemoveRenderModule(Identification renderModuleId)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void SetRenderModuleActive(Identification renderModuleId, bool active)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public bool IsRenderModuleActive(Identification renderModuleId)
    {
        throw new System.NotImplementedException();
    }
}