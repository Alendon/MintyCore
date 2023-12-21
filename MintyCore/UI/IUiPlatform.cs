using Myra.Platform;
using Silk.NET.Maths;

namespace MintyCore.UI;

public interface IUiPlatform : IMyraPlatform
{
    void Resize(Vector2D<int> obj);
}