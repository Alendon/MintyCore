using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Implementations;

[Singleton<IIntermediateManager>(SingletonContextFlags.NoHeadless)]
public class IntermediateManager(IInputManager inputManager) : IIntermediateManager
{
    
}