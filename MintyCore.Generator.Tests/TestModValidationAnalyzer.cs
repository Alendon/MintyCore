using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MintyCoreGenerator;
using static MintyCore.Generator.Tests.SourceGenHelper;

namespace MintyCore.Generator.Tests;

public class TestModValidationAnalyzer
{
    private const string IModInterface = """
namespace MintyCore.Modding;

public interface IMod
{
    void Dispose();
    ushort ModId { get; set; }
    void PreLoad();
    void Load();
    void PostLoad();
    void Unload();
}
""";

    [Fact]
    public void ModValidationAnalyzer_ShouldNotReportDiagnostic()
    {
        var testCode = """
using MintyCore.Modding;
namespace TestMod;
public sealed partial class Test1 : IMod
{
    public void Dispose() { }
    public ushort ModId { get; set; }
    public void PreLoad() { }
    public void Load() { }
    public void PostLoad() { }
    public void Unload() { }
}
""";
        DiagnosticAnalyzer analyzer = new ModValidationAnalyzer();

        Analyze(analyzer, out var diagnostics, testCode, IModInterface);

        Assert.Empty(diagnostics);
    }


    [Fact]
    public void ModValidationAnalyzer_Public_ShouldReportDiagnostic()
    {
        var testCode = """
using MintyCore.Modding;
namespace TestMod;
private sealed partial class Test1 : IMod
{
    public void Dispose() { }
    public ushort ModId { get; set; }
    public void PreLoad() { }
    public void Load() { }
    public void PostLoad() { }
    public void Unload() { }
}
""";

        Analyze(new ModValidationAnalyzer(), out var diagnostics, testCode, IModInterface);

        Assert.Single(diagnostics);
        Assert.True(diagnostics[0].Id.Equals("MC2101"));
        Assert.True(diagnostics[0].Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void ModValidationAnalyzer_Sealed_ShouldReportDiagnostic()
    {
        var testCode = """
using MintyCore.Modding;
namespace TestMod;
public partial class Test1 : IMod
{
    public void Dispose() { }
    public ushort ModId { get; set; }
    public void PreLoad() { }
    public void Load() { }
    public void PostLoad() { }
    public void Unload() { }
}
""";

        Analyze(new ModValidationAnalyzer(), out var diagnostics, testCode, IModInterface);

        Assert.Single(diagnostics);
        Assert.True(diagnostics[0].Id.Equals("MC2102"));
        Assert.True(diagnostics[0].Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void ModValidationAnalyzer_Partial_ShouldReportDiagnostic()
    {
        var testCode = """
using MintyCore.Modding;
namespace TestMod;
public sealed class Test1 : IMod
{
    public void Dispose() { }
    public ushort ModId { get; set; }
    public void PreLoad() { }
    public void Load() { }
    public void PostLoad() { }
    public void Unload() { }
}
""";

        Analyze(new ModValidationAnalyzer(), out var diagnostics, testCode, IModInterface);

        Assert.Single(diagnostics);
        Assert.True(diagnostics[0].Id.Equals("MC2103"));
        Assert.True(diagnostics[0].Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void ModValidationAnalyzer_NoMod_ShouldReportDiagnostic()
    {
        Analyze(new ModValidationAnalyzer(), out var diagnostics, IModInterface);

        Assert.Single(diagnostics);
        Assert.True(diagnostics[0].Id.Equals("MC2202"));
        Assert.True(diagnostics[0].Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ModValidationAnalyzer_OnlyOne_ShouldReportDiagnostic()
    {
        var testCode = """
using MintyCore.Modding;
namespace TestMod;
public partial sealed class Test1 : IMod
{
    public void Dispose() { }
    public ushort ModId { get; set; }
    public void PreLoad() { }
    public void Load() { }
    public void PostLoad() { }
    public void Unload() { }
}

using MintyCore.Modding;
namespace TestMod;
public partial sealed class Test2 : IMod
{
    public void Dispose() { }
    public ushort ModId { get; set; }
    public void PreLoad() { }
    public void Load() { }
    public void PostLoad() { }
    public void Unload() { }
}
""";

        Analyze(new ModValidationAnalyzer(), out var diagnostics, testCode, IModInterface);


        Assert.Single(diagnostics);
        Assert.True(diagnostics[0].Id.Equals("MC2201"));
        Assert.True(diagnostics[0].Severity == DiagnosticSeverity.Error);
    }
}