using Microsoft.CodeAnalysis.CSharp;
using MintyCore.Generator.SendMessage;
using MintyCore.Generator.Tests.CommonTemplates;
using SharedCode;

namespace MintyCore.Generator.Tests.SendMessageTests;

public class SendMessageGeneratorTests
{
    private const string TestTemplateDir = "MintyCore.Generator.Tests.SendMessageTests.TestTemplates";
    
    [Fact]
    public void SendMessageGenerator_ShouldGenerateCorrectCode()
    {
        var testCode = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TestTemplateDir}.Input_Valid.sbncs");
        Assert.NotNull(testCode);

        var expectedCode = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TestTemplateDir}.Output_Valid.sbncs");
        Assert.NotNull(expectedCode);

        var expectedTree = CSharpSyntaxTree.ParseText(expectedCode);

        Compile(new SendMessageGenerator(), out _, out var diagnostics, out var generatedTrees,
            testCode, MessageInterface);

        Assert.Empty(diagnostics);
        Assert.Single(generatedTrees);
        Assert.True(expectedTree.IsEquivalentTo(generatedTrees[0]));
    }

    [Fact]
    public void SendMessageGenerator_NoPartial_ShouldNotGenerateCode()
    {
        var testCode = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TestTemplateDir}.Input_NoPartial.sbncs");
        Assert.NotNull(testCode);

        Compile(new SendMessageGenerator(), out _, out var diagnostics, out var generatedTrees,
            testCode, MessageInterface);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void SendMessageGenerator_NestedClass_ShouldNotGenerateCode()
    {
        var testCode = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TestTemplateDir}.Input_NestedClass.sbncs");
        Assert.NotNull(testCode);

        Compile(new SendMessageGenerator(), out _, out var diagnostics, out var generatedTrees,
            testCode, MessageInterface);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }
}