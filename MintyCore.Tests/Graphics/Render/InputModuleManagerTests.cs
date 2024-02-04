using Autofac;
using JetBrains.Annotations;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Graphics.Render.Managers.Implementations;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Tests.Graphics.Render;

public class InputModuleManagerTests
{
    private readonly IInputModuleManager _inputModuleManager = new InputModuleManager(new ContainerBuilder().Build());
    private static readonly Identification _moduleTestId = new(1, 2, 3);


    [Fact]
    public void RegisterModule_Valid_ShouldThrowNoException()
    {
        var act = () => _inputModuleManager.RegisterInputModule<TestModule>(_moduleTestId);

        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterModule_Duplicate_ShouldThrowException()
    {
        _inputModuleManager.RegisterInputModule<TestModule>(_moduleTestId);

        var act = () => _inputModuleManager.RegisterInputModule<TestModule>(_moduleTestId);

        act.Should().Throw<MintyCoreException>()
            .WithMessage($"Input Data Module for {_moduleTestId} is already registered");
    }

    [Fact]
    public void RegisteredInputModuleIds_ShouldContainRegisteredModuleId()
    {
        _inputModuleManager.RegisterInputModule<TestModule>(_moduleTestId);

        _inputModuleManager.RegisteredInputModuleIds.Should().Contain(_moduleTestId);
    }

    [Fact]
    public void CreateInputModuleInstances_ShouldCreateInstances_WithRegisteredModules()
    {
        _inputModuleManager.RegisterInputModule<TestModule>(_moduleTestId);

        var createdModules = _inputModuleManager.CreateInputModuleInstances(out var container);

        createdModules.Should().ContainSingle().Which.Value.Should().BeOfType<TestModule>();

        container.Dispose();
    }

    [Fact]
    public void CreateInputModuleInstances_DeactivatedModule_ShouldNotCreateInstance()
    {
        _inputModuleManager.RegisterInputModule<TestModule>(_moduleTestId);
        _inputModuleManager.SetModuleActive(_moduleTestId, false);

        var createdModules = _inputModuleManager.CreateInputModuleInstances(out var container);

        createdModules.Should().BeEmpty();
        
        container.Dispose();
    }

    [UsedImplicitly]
    private class TestModule : InputModule
    {
        public override void Setup()
        {
        }

        public override void Update(ManagedCommandBuffer commandBuffer)
        {
        }

        public override Identification Identification => _moduleTestId;

        public override void Dispose()
        {
        }
    }
}