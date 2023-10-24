using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace TestMod.Render;

public class FillColorOutput : IRenderOutputWrapper<Texture>
{
    private readonly Texture _internalTexture;

    public FillColorOutput(Texture texture)
    {
        _internalTexture = texture;
    }
    
 

    /// <inheritdoc />
    public Texture GetConcreteOutput()
    {
        return _internalTexture;
    }

    /// <inheritdoc />
    public object GetOutput()
    {
        return GetConcreteOutput();
    }
}

public class FillColor : IRenderModuleOutput<FillColorOutput>
{
    public required ITextureManager TextureManager { private get; init; }
    public required IVulkanEngine VulkanEngine { private get; init; }
    
    /// <inheritdoc />
    public void Dispose()
    {
        
    }

    /// <inheritdoc />
    public void Process()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Initialize()
    {
        //Framebuffer and Renderpass!!!
        //Handle Framebuffer resize
        
        
        
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public FillColorOutput GetOrCreateOutput()
    {
        var textureDesc = TextureDescription.Texture2D(VulkanEngine.SwapchainExtent.Width,
            VulkanEngine.SwapchainExtent.Height, 1, 1, VulkanEngine.SwapchainImageFormat, TextureUsage.RenderTarget);

        var texture = TextureManager.Create(ref textureDesc);

        return new FillColorOutput(texture);
    }

    /// <inheritdoc />
    public void CleanupOutput(FillColorOutput output)
    {
        output.Dispose();
    }

    /// <inheritdoc />
    public static Identification GetOutputId()
    {
        throw new NotImplementedException();
    }
}