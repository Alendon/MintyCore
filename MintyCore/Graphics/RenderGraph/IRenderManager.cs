using MintyCore.Graphics.RenderGraph.RenderResources;
using MintyCore.Utils;

namespace MintyCore.Graphics.RenderGraph;

public interface IRenderManager
{
    public void AddRenderModule(Identification id, RenderModuleDescription renderModuleDescription);
    
    public void AddTextureResource(Identification id, TextureResourceDescription textureResourceDescription);
    public void SetSwapchainResource(Identification id);

    public void SetRenderModuleActive(Identification id, bool active);
    public bool IsRenderModuleActive(Identification id);
    
    public void ConstructRenderGraph();
    public void BeginRendering();
    public void EndRendering();
    
    public int FPS { get; set; }
    
    public void Dispose();
}