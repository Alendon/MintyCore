using MintyCore.Utils;
using Myra.Platform;

namespace MintyCore.UI;

public interface IUiRenderer : IMyraRenderer
{
    DisposeActionWrapper GetCurrentRenderData(out UiRenderData renderData);
    void SwapRenderData();
}