using MintyCore.Utils;
using Myra.Graphics2D;
using Myra.Platform;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

public interface IUiRenderer : IMyraRenderer
{
    DisposeActionWrapper GetCurrentRenderData(out UiRenderData renderData);
    void SwapRenderData();
}