using MintyCore.Generator.SendMessage;
using MintyCore.Generator.Tests.CommonTemplates;
using SharedCode;

namespace MintyCore.Generator.Tests.SendMessageTests;

public class SendMessageAnalyzerTests
{
    private const string TestTemplateDir = "MintyCore.Generator.Tests.SendMessageTests.TestTemplates";


    [Fact]
    public void SendMessageAnalyzer_ShouldReportNoError()
    {
        var testCode = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TestTemplateDir}.Input_Valid.sbncs");
        Assert.NotNull(testCode);

        Analyze(new SendMessageAnalyzer(), out var diagnostics,
            testCode, MessageInterface);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void SendMessageAnalyzer_NoPartial_ShouldReportWarning()
    {
        var testCode = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TestTemplateDir}.Input_NoPartial.sbncs");
        Assert.NotNull(testCode);

        Analyze(new SendMessageAnalyzer(), out var diagnostics,
            testCode, MessageInterface);

        Assert.Single(diagnostics);
        Assert.Equal("MC3102", diagnostics[0].Id);
    }

    [Fact]
    public void SendMessageAnalyzer_Nested_ShouldReportWarning()
    {
        var testCode = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TestTemplateDir}.Input_NestedClass.sbncs");
        Assert.NotNull(testCode);

        Analyze(new SendMessageAnalyzer(), out var diagnostics,
            testCode, MessageInterface);

        Assert.Single(diagnostics);
        Assert.Equal("MC3101", diagnostics[0].Id);
    }
}