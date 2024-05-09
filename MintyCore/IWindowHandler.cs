using MintyCore.Utils;

namespace MintyCore;

public interface IWindowHandler
{
    void CreateMainWindow();
    void DestroyMainWindow();

    Window GetMainWindow();
}