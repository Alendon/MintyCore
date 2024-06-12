using JetBrains.Annotations;
using MintyCore;
using MintyCore.Graphics;
using MintyCore.Input;
using MintyCore.Modding;
using MintyCore.Registries;
using Serilog;
using Silk.NET.GLFW;
using TestMod.Identifications;
using ComponentIDs = TestMod.Identifications.ComponentIDs;

namespace TestMod;

[UsedImplicitly]
public sealed class Test : IMod
{
    public required IVulkanEngine VulkanEngine { [UsedImplicitly] init; private get; }
    public required ITestDependency TestDependency { [UsedImplicitly] init; private get; }
    public required IEngineConfiguration EngineConfiguration { [UsedImplicitly] init; private get; }

    public void Dispose()
    {
        //Nothing to do here
    }

    public void PreLoad()
    {
        TestDependency.DoSomething();

        VulkanEngine.DeviceFeaturesVulkan11 = VulkanEngine.DeviceFeaturesVulkan11 with
        {
            ShaderDrawParameters = true
        };
    }
    

    [RegisterArchetype("test")]
    public static ArchetypeInfo TestArchetype() => new()
    {
        Ids = new[]
        {
            ComponentIDs.Test,
        }
    };


    public void Load()
    {
    }

    public void PostLoad()
    {
        EngineConfiguration.DefaultGameState = GameStateIDs.MainMenu;
    }

    public void Unload()
    {
    }

    

    [RegisterInputAction("test_without_modifiers")]
    public static InputActionDescription TestInputWithoutModifiers() => new()
    {
        DefaultInput = Keys.G,
        ActionCallback = inputActionParams =>
        {
            if (inputActionParams.InputAction == InputAction.Press)
                Log.Information("Test Input without modifiers, active: {Modifiers}", inputActionParams.ActiveModifiers);
            return InputActionResult.Stop;
        }
    };

    [RegisterInputAction("test_with_ctrl")]
    public static InputActionDescription TestInputWithCtrl() => new()
    {
        DefaultInput = Keys.G,
        ActionCallback = inputActionParams =>
        {
            if (inputActionParams.InputAction == InputAction.Press)
                Log.Information("Test Input with ctrl, active: {Modifiers}", inputActionParams.ActiveModifiers);
            return InputActionResult.Continue;
        },
        RequiredModifiers = KeyModifiers.Control
    };

    [RegisterInputAction("test_with_shift")]
    public static InputActionDescription TestInputWithShift() => new()
    {
        DefaultInput = Keys.G,
        ActionCallback = inputActionParams =>
        {
            if (inputActionParams.InputAction == InputAction.Press)
                Log.Information("Test Input with shift, active: {Modifiers}", inputActionParams.ActiveModifiers);
            return InputActionResult.Continue;
        },
        RequiredModifiers = KeyModifiers.Shift
    };

    [RegisterInputAction("test_with_both")]
    public static InputActionDescription TestInputWithBothModifiers() => new()
    {
        DefaultInput = Keys.G,
        ActionCallback = inputActionParams =>
        {
            if (inputActionParams.InputAction == InputAction.Press)
                Log.Information("Test Input with both, active: {Modifiers}", inputActionParams.ActiveModifiers);
            return InputActionResult.Continue;
        },
        RequiredModifiers = KeyModifiers.Shift | KeyModifiers.Control
    };
}