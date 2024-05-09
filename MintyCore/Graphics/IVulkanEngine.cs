using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace MintyCore.Graphics;

/// <summary>
/// The main access point to the vulkan api
/// </summary>
[PublicAPI]
public interface IVulkanEngine : IDisposable
{
    /// <summary>
    /// Whether or not the validation layers are active
    /// </summary>
    bool ValidationLayersActive { get; }
    
    /// <summary>
    /// The main access point to the vulkan api
    /// </summary>
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
    (Queue queue, object queueLock, uint familyIndex) GraphicQueue { get; }

    /// <summary>
    ///     The vulkan present queue (possible the same as <see cref="GraphicQueue" />)
    /// </summary>
    (Queue queue, object queueLock, uint familyIndex) PresentQueue { get; }

    /// <summary>
    ///     The vulkan compute queue
    /// </summary>
    (Queue graphicQueue, object queueLock, uint familyIndex) ComputeQueue { get; }

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
    ///     Command pools  for graphic commands
    /// </summary>
    ManagedCommandPool[] GraphicsCommandPool { get; }

    /// <summary>
    ///     Whether or not drawing is enabled
    /// </summary>
    bool DrawEnable { get; }

    /// <summary>
    ///     The current Image index
    /// </summary>
    uint SwapchainImageIndex { get; }
    
    /// <summary>
    /// Current render index
    /// May be different from <see cref="SwapchainImageIndex"/>
    /// </summary>
    uint RenderIndex { get; }

    /// <summary>
    /// Recreate the swapchain
    /// </summary>
    void RecreateSwapchain();

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

    /// <summary>
    /// Initialize the vulkan engine
    /// </summary>
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
    /// <returns>Secondary command buffer</returns>
    ManagedCommandBuffer GetSecondaryCommandBuffer();

    /// <summary>
    /// Get the current render command buffer
    /// </summary>
    ManagedCommandBuffer GetRenderCommandBuffer();

    /// <summary>
    ///     Execute a secondary command buffer on the graphics command buffer
    /// </summary>
    /// <param name="buffer">Command buffer to execute</param>
    void ExecuteSecondary(ManagedCommandBuffer buffer);

    /// <summary>
    /// Add a semaphore which will be added to the next submit call
    /// The semaphore will be waited on before the command buffers will be executed
    /// </summary>
    /// <param name="semaphore"> The semaphore to wait on</param>
    /// <param name="waitStage"> The pipeline stage the semaphore will be waited on</param>
    /// <see href="https://registry.khronos.org/vulkan/specs/1.3-extensions/man/html/VkSubmitInfo.html">Vulkan Submit Info</see>
    /// <see href="https://registry.khronos.org/vulkan/specs/1.3-extensions/man/html/VkSemaphore.html">Vulkan Semaphore</see>
    /// <remarks> This only applies to the next submit call. </remarks>
    void AddSubmitWaitSemaphore(Semaphore semaphore, PipelineStageFlags waitStage);
    
    /// <summary>
    /// Add a element which will be added to the pNext chain on the next submit call
    /// </summary>
    /// <param name="pNext">Pointer to the element</param>
    /// <remarks> This only applies to the next submit call. </remarks>
    void AddSubmitPNext(IntPtr pNext);
    
    /// <summary>
    /// Add a semaphore which will be signaled after all command buffers of the next submit call have been executed
    /// </summary>
    /// <param name="semaphore"> The semaphore to signal</param>
    /// <remarks> This only applies to the next submit call. </remarks>
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

    /// <summary>
    /// Cleanup the swapchain (destroy all resources)
    /// </summary>
    void CleanupSwapchain();

    /// <summary>
    ///     Wait for the completion of every running gpu process
    /// </summary>
    void WaitForAll();

    /// <summary>
    ///     Get a command buffer for single time execution
    /// </summary>
    /// <returns>Single time command buffer</returns>
    ManagedCommandBuffer GetSingleTimeCommandBuffer();

    /// <summary>
    ///     Execute a pre fetched single time command buffer
    /// </summary>
    /// <param name="buffer"></param>
    void ExecuteSingleTimeCommandBuffer(ManagedCommandBuffer buffer);

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
        ManagedCommandBuffer cb,
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