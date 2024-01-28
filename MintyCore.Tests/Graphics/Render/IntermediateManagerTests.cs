using Autofac;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Implementations;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class IntermediateManagerTests : IDisposable
{
    private readonly IInputManager _inputManager;
    private readonly IIntermediateManager _intermediateManager;
    private readonly IContainer _container;

    private readonly Identification inputModuleId;
    private readonly Identification intermediateDataId;

    public IntermediateManagerTests()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<InputManager>().As<IInputManager>();
        builder.RegisterType<IntermediateManager>().As<IIntermediateManager>();

        _container = builder.Build();
        _inputManager = _container.Resolve<IInputManager>();
        _intermediateManager = _container.Resolve<IIntermediateManager>();
        
        inputModuleId = new Identification(1, 2, 3);
        intermediateDataId = new Identification(4, 5, 6);
    }

    public void Dispose()
    {
        _container.Dispose();
    }

    [Fact]
    public void SetIntermediateProvider_ShouldNotThrow()
    {
        
    }
}