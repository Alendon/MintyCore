using JetBrains.Annotations;

namespace MintyCore.Graphics.Render.Managers;

/// <summary>
///   Interface for the render manager.
/// </summary>
[PublicAPI]
public interface IRenderManager
{
    /// <summary>
    /// Starts the rendering process.
    /// Throws an InvalidOperationException if the rendering process is already running.
    /// </summary>
    void StartRendering();

    /// <summary>
    /// Stops the rendering process.
    /// Logs an error if the rendering process is not running.
    /// </summary>
    void StopRendering();

    /// <summary>
    /// Gets or sets the maximum frame rate for the rendering process.
    /// If the rendering process is running, the change will be applied immediately.
    /// </summary>
    int MaxFrameRate { get; set; }

    /// <summary>
    /// Gets the current frame rate of the rendering process.
    /// Returns 0 if the rendering process is not running.
    /// </summary>
    int FrameRate { get; }
}