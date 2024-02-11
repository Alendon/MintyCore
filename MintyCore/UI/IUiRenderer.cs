using Myra.Platform;

namespace MintyCore.UI;

public interface IUiRenderer : IMyraRenderer
{
    void ApplyRenderData();
}