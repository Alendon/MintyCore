using MintyCore.GameStates;
using MintyCore.Input;
using MintyCore.Utils;

namespace MintyCore;

[Singleton<IWindowHandler>(SingletonContextFlags.NoHeadless)]
public class WindowHandler(
    IInputHandler inputHandler,
    IEngineConfiguration engineConfiguration,
    IGameStateMachine gameStateMachine) : IWindowHandler
{
    private Window? _window;

    public void CreateMainWindow()
    {
        _window = new Window(inputHandler, engineConfiguration, gameStateMachine);
    }

    public void DestroyMainWindow()
    {
        _window?.WindowInstance.Reset();
        _window = null;
    }

    public Window GetMainWindow()
    {
        return _window ?? throw new MintyCoreException("Main window not created");
    }
}