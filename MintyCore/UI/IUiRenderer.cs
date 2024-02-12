using Myra.Platform;

namespace MintyCore.UI;

/// <summary>
///  Interface for the ui renderer
/// </summary>
public interface IUiRenderer : IMyraRenderer
{
    /// <summary>
    ///  Applies the render data
    /// </summary>
    void ApplyRenderData();
}