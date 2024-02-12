using Myra.Platform;
using Silk.NET.Maths;

namespace MintyCore.UI;

/// <summary>
///   Interface for the ui platform
/// </summary>
public interface IUiPlatform : IMyraPlatform
{
    /// <summary>
    ///  Resizes the UI
    /// </summary>
    void Resize(Vector2D<int> obj);
}