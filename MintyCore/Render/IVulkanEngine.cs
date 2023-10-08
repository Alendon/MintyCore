using System;
using System.Collections.Generic;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace MintyCore.Render;

public interface IVulkanEngine
{
    bool ValidationLayersActive { get; }
    IAllocationHandler AllocationHandler { init; }
    
    Vk Vk { get; }

    /// <summary>
    ///     The current vulkan instance
    /// </summary>
    Instance Instance { get; }

    /// <summary>
    ///     The current vulkan surface
    /// </summary>
    SurfaceKHR Surface { get; }

    /// <summary>
    ///     The current vulkan physical device
    /// </summary>
    PhysicalDevice PhysicalDevice { get; }

    /// <summary>
    ///     The current vulkan physical device memory properties
    /// </summary>
    PhysicalDeviceMemoryProperties PhysicalDeviceMemoryProperties { get; }

    /// <summary>
    ///     The vulkan logical device
    /// </summary>
    Device Device { get; }

    /// <summary>
    ///     The information about queue family indices
    /// </summary>
    QueueFamilyIndexes QueueFamilyIndexes { get; }

    /// <summary>
    ///     The vulkan graphics queue
    /// </summary>
    (Queue queue, object queueLock) GraphicQueue { get; }

    /// <summary>
    ///     The vulkan present queue (possible the same as <see cref="GraphicQueue" />)
    /// </summary>
    (Queue queue, object queueLock) PresentQueue { get; }

    /// <summary>
    ///     The vulkan compute queue
    /// </summary>
    (Queue graphicQueue, object queueLock) ComputeQueue { get; }

    /// <summary>
    ///     The vulkan surface api access
    /// </summary>
// ReSharper disable once NotNullMemberIsNotInitialized
    KhrSurface? VkSurface { get; }

    /// <summary>
    ///     The vulkan swapchain api access
    /// </summary>
// ReSharper disable once NotNullMemberIsNotInitialized
    KhrSwapchain? VkSwapchain { get; }

    /// <summary>
    ///     The vulkan swapchain
    /// </summary>
    SwapchainKHR Swapchain { get; }

    /// <summary>
    ///     The vulkan swapchain images
    /// </summary>
    Image[] SwapchainImages { get; }

    /// <summary>
    ///     The swapchain image format
    /// </summary>
    Format SwapchainImageFormat { get; }

    /// <summary>
    ///     The swapchain extent (size)
    /// </summary>
    Extent2D SwapchainExtent { get; }

    /// <summary>
    ///     The swapchain image views
    /// </summary>
    ImageView[] SwapchainImageViews { get; }

    /// <summary>
    ///     The swapchain image count.
    ///     Useful if you want to have per frame data on the gpu (like dynamic data)
    /// </summary>
    int SwapchainImageCount { get; }

    /// <summary>
    ///     The depth texture
    /// </summary>
    Texture? DepthTexture { get; }

    /// <summary>
    ///     The depth image view
    /// </summary>
    ImageView DepthImageView { get; }

    /// <summary>
    ///     the framebuffers of the swap chains
    /// </summary>
    Framebuffer[] SwapchainFramebuffers { get; }

    /// <summary>
    ///     Command pools  for graphic commands
    /// </summary>
    CommandPool[] GraphicsCommandPool { get; }

    /// <summary>
    ///     Whether or not drawing is enabled
    /// </summary>
    bool DrawEnable { get; }

    /// <summary>
    ///     The current Image index
    /// </summary>
    uint ImageIndex { get; }

    /// <summary>
    /// List of loaded device extensions
    /// </summary>
    IReadOnlySet<string> LoadedDeviceExtensions { get; }

    /// <summary>
    /// List of all loaded instance layers.
    /// </summary>
    IReadOnlySet<string> LoadedInstanceLayers { get; }

    /// <summary>
    /// List of all loaded instance extensions.
    /// </summary>
    IReadOnlySet<string> LoadedInstanceExtensions { get; }

    void Setup();

    /// <summary>
    ///     Prepare the current frame for drawing
    /// </summary>
    /// <returns>True if the next image could be acquired. If false do no rendering</returns>
    bool PrepareDraw();

    /// <summary>
    ///     Get secondary command buffer for rendering
    ///     CommandBuffers acquired with this method are only valid for the current frame and be returned to the internal pool
    /// </summary>
    /// <param name="beginBuffer">Whether or not the buffer should be started</param>
    /// <param name="inheritRenderPass">Whether or not the render pass should be inherited</param>
    /// <param name="renderPass"></param>
    /// <param name="subpass"></param>
    /// <returns>Secondary command buffer</returns>
    CommandBuffer GetSecondaryCommandBuffer(bool beginBuffer = true, bool inheritRenderPass = true,
        RenderPass renderPass = default, uint subpass = 0);

    /// <summary>
    ///     Execute a secondary command buffer on the graphics command buffer
    /// </summary>
    /// <param name="buffer">Command buffer to execute</param>
    /// <param name="endBuffer">Whether or not the command buffer need to be ended</param>
    void ExecuteSecondary(CommandBuffer buffer, bool endBuffer = true);

    /// <summary>
    /// Set the currently active render pass for the main command buffer
    /// </summary>
    /// <param name="renderPass"><see cref="RenderPassBeginInfo.RenderPass"/></param>
    /// <param name="subpassContents"></param>
    /// <param name="clearValues"><see cref="RenderPassBeginInfo.PClearValues"/></param>
    /// <param name="renderArea"><see cref="RenderPassBeginInfo.RenderArea"/></param>
    /// <param name="framebuffer"><see cref="RenderPassBeginInfo.Framebuffer"/></param>
    void SetActiveRenderPass(RenderPass renderPass, SubpassContents subpassContents,
        Span<ClearValue> clearValues = default,
        Rect2D? renderArea = null, Framebuffer? framebuffer = null);

    /// <summary>
    /// Increase to the next subpass of the currently active render pass
    /// </summary>
    /// <param name="subPassContents"></param>
    void NextSubPass(SubpassContents subPassContents);

    void AddSubmitWaitSemaphore(Semaphore semaphore, PipelineStageFlags waitStage);
    void AddSubmitPNext(IntPtr pNext);
    void AddSubmitSignalSemaphore(Semaphore semaphore);

    /// <summary>
    ///     End the draw of the current frame
    /// </summary>
    void EndDraw();

    /// <summary>
    /// Add a device extension
    /// </summary>
    /// <param name="modName">The mod adding the extension</param>
    /// <param name="extensionName">The name of the extension</param>
    /// <param name="hardRequirement">Whether the extension is a hard requirement. A exception will be thrown if the extension is not found</param>
    void AddDeviceExtension(string modName, string extensionName, bool hardRequirement);

    /// <summary>
    ///  Add a device feature extension
    /// </summary>
    /// <param name="extension"> The extension to add</param>
    /// <typeparam name="TExtension"> The type of the extension</typeparam>
    void AddDeviceFeatureExension<TExtension>(TExtension extension)
        where TExtension : unmanaged, IChainable;

    /// <summary>
    /// Event called right before the device is created
    /// Remember to unsubscribe to don't break mod unloading
    /// </summary>
    event Action OnDeviceCreation;

    /// <summary>
    /// Add a layer to be used by the instance.
    /// </summary>
    /// <param name="modName"> The name of the mod requesting the layer.</param>
    /// <param name="layers"> The name of the layer.</param>
    /// <param name="hardRequirement"> Whether the layer is a hard requirement. If yes a exception will be thrown if the layer is not available.</param>
    void AddInstanceLayer(string modName, string layers, bool hardRequirement = true);

    /// <summary>
    /// Add an extension to be used by the instance.
    /// </summary>
    /// <param name="modName"> The name of the mod requesting the extension.</param>
    /// <param name="extensions"> The name of the extension.</param>
    /// <param name="hardRequirement"> Whether the extension is a hard requirement. If yes a exception will be thrown if the extension is not available.</param>
    void AddInstanceExtension(string modName, string extensions, bool hardRequirement = true);

    /// <summary>
    /// Add a instance feature extension
    /// </summary>
    /// <param name="extension">The extension to use</param>
    /// <typeparam name="TExtension">The type of the extension</typeparam>
    void AddInstanceFeatureExtension<TExtension>(TExtension extension)
        where TExtension : unmanaged, IChainable;

    void CleanupSwapchain();
    void Shutdown();

    /// <summary>
    ///     Wait for the completion of every running gpu process
    /// </summary>
    void WaitForAll();

    /// <summary>
    ///     Get a command buffer for single time execution
    /// </summary>
    /// <returns>Single time command buffer</returns>
    CommandBuffer GetSingleTimeCommandBuffer();

    /// <summary>
    ///     Execute a pre fetched single time command buffer
    /// </summary>
    /// <param name="buffer"></param>
    void ExecuteSingleTimeCommandBuffer(CommandBuffer buffer);

    /// <summary>
    ///     Clear the color texture
    /// </summary>
    /// <param name="texture">The texture to clear</param>
    /// <param name="clearColorValue">The clear value</param>
    void ClearColorTexture(Texture texture, ClearColorValue clearColorValue);

    /// <summary>
    ///     Clear a depth texture
    /// </summary>
    /// <param name="texture">Texture to clear</param>
    /// <param name="clearDepthStencilValue">Clear value</param>
    void ClearDepthTexture(Texture texture, ClearDepthStencilValue clearDepthStencilValue);

    /// <summary>
    ///     Transition the layout of the texture
    /// </summary>
    /// <param name="texture">Texture to transition</param>
    /// <param name="layout">New layout for the texture</param>
    void TransitionImageLayout(Texture texture, ImageLayout layout);

    /// <summary>
    /// </summary>
    /// <param name="cb"></param>
    /// <param name="image"></param>
    /// <param name="baseMipLevel"></param>
    /// <param name="levelCount"></param>
    /// <param name="baseArrayLayer"></param>
    /// <param name="layerCount"></param>
    /// <param name="aspectMask"></param>
    /// <param name="oldLayout"></param>
    /// <param name="newLayout"></param>
    void TransitionImageLayout(
        CommandBuffer cb,
        Image image,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        ImageAspectFlags aspectMask,
        ImageLayout oldLayout,
        ImageLayout newLayout);

    /// <summary>
    ///     Enumerate device extensions
    /// </summary>
    /// <param name="device">Device to enumerate</param>
    /// <param name="layer">Optional to get layer information</param>
    /// <returns>Available extensions</returns>
    unsafe string[] EnumerateDeviceExtensions(PhysicalDevice device, byte* layer = null);

    /// <summary>
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
    unsafe string[] EnumerateInstanceExtensions(byte* layerName = null);

    /// <summary>
    /// </summary>
    /// <returns></returns>
    string[] EnumerateInstanceLayers();

    /// <summary>
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    PhysicalDevice[] EnumerateDevices(Instance instance);

    /// <summary>
    /// </summary>
    /// <param name="typeFilter"></param>
    /// <param name="requiredFlags"></param>
    /// <param name="memoryTypeIndex"></param>
    /// <returns></returns>
    bool FindMemoryType(uint typeFilter, MemoryPropertyFlags requiredFlags, out uint memoryTypeIndex);

    /// <summary>
    /// Check if the vulkan instance is valid
    /// <exception cref="MintyCoreException">No valid vulkan instance is available</exception>
    /// </summary>
    void AssertVulkanInstance();
}