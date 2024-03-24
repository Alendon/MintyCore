namespace MintyCore.Generator.Tests;

public class TestWorldImplementationAnalyzer
{
    private const string WorldInterfaceDummyImplementation = """
                                                             namespace MintyCore.ECS;
                                                             public interface IWorld { }
                                                             """;
    private const string RegisterAttributeDummyImplementation = """
                                                                 namespace MintyCore.Registries;
                                                                 public class RegisterWorldAttribute : Attribute { 
                                                                     public RegisterWorldAttribute(string id) { }
                                                                 }
                                                                 """;
    
    [Fact]
    public void WorldImplementationAnalyzer_HasIsServerWorldParameter_ShouldReportNoError()
    {
        var testCode = """
                       [RegisterWorld("test")]
                       public sealed class TestWorld : IWorld
                       {
                            public TestWorld(object someParam, bool isServerWorld, int someOtherParam) { }
                       }
                       """;

        Analyze(new WorldImplementationAnalyzer(), out var diagnostics,
            testCode, WorldInterfaceDummyImplementation, RegisterAttributeDummyImplementation);

        Assert.Empty(diagnostics);
    }
    
    [Fact]
    public void WorldImplementationAnalyzer_WithoutIsServerWorldParameter_ShouldReportError()
    {
        var testCode = """
                       using MintyCore.ECS;
                       using MintyCore.Registries;
                       
                       [RegisterWorld("test")]
                       public sealed class TestWorld : IWorld
                       {
                            public TestWorld(object someParam, int someOtherParam) { }
                       }
                       """;

        Analyze(new WorldImplementationAnalyzer(), out var diagnostics,
            testCode, WorldInterfaceDummyImplementation, RegisterAttributeDummyImplementation);

        var diagnostic = Assert.Single(diagnostics);
        
        Assert.Equal("MC3201", diagnostic.Id);
    }
    
    [Fact]
    public void WorldImplementationAnalyzer_MultipleFaultyConstructors_ShouldReportMultipleErrors()
    {
        var testCode = """
                       using MintyCore.ECS;
                       using MintyCore.Registries;

                       [RegisterWorld("test")]
                       public sealed class TestWorld : IWorld
                       {
                            public TestWorld(object someParam, int someOtherParam) { }
                            public TestWorld(object someParam) { }
                       }
                       """;

        Analyze(new WorldImplementationAnalyzer(), out var diagnostics,
            testCode, WorldInterfaceDummyImplementation, RegisterAttributeDummyImplementation);

        Assert.Equal(2, diagnostics.Count());
        
        Assert.All(diagnostics, diagnostic => Assert.Equal("MC3201", diagnostic.Id));
    }
    
    [Fact]
    public void WorldImplementationAnalyzer_DefaultConstructors_ShouldReportError()
    {
        var testCode = """
                       using MintyCore.ECS;
                       using MintyCore.Registries;

                       [RegisterWorld("test")]
                       public sealed class TestWorld : IWorld
                       {
                       }
                       """;

        Analyze(new WorldImplementationAnalyzer(), out var diagnostics,
            testCode, WorldInterfaceDummyImplementation, RegisterAttributeDummyImplementation);

        var diagnostic = Assert.Single(diagnostics);
        
        Assert.Equal("MC3201", diagnostic.Id);
    }
    
    [Fact]
    public void WorldImplementationAnalyzer_WithoutRegistration_ShouldReportNoError()
    {
        var testCode = """
                       using MintyCore.ECS;
                       using MintyCore.Registries;

                       public sealed class TestWorld : IWorld
                       {
                            public TestWorld(object someParam, int someOtherParam) { }
                       }
                       """;

        Analyze(new WorldImplementationAnalyzer(), out var diagnostics,
            testCode, WorldInterfaceDummyImplementation, RegisterAttributeDummyImplementation);

        Assert.Empty(diagnostics);
    }
}