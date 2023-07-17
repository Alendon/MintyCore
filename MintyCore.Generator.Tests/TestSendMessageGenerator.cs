using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MintyCore.Network;
using MintyCoreGenerator;

namespace MintyCore.Generator.Tests;

public partial class TestSendMessageGenerator
{
    private static void Compile(string source, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("test_compilation", new[] {syntaxTree},
            new[] {MetadataReference.CreateFromFile(typeof(IMessage).GetTypeInfo().Assembly.Location)},
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SendMessageGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => t != syntaxTree).ToArray();
    }

    [System.Text.RegularExpressions.GeneratedRegex("\\s+")]
    private static partial System.Text.RegularExpressions.Regex WhiteSpaceRegex();

    private static bool CodeMatch(string expected, string actual)
    {
        //replace all whitespace characters with a single space
        //these includes, tabs, whitespaces, newlines, etc.
        expected = WhiteSpaceRegex().Replace(expected, " ");
        actual = WhiteSpaceRegex().Replace(actual, " ");
        
        return expected == actual;
    }

    [Fact]
    public void SendMessageGenerator_ShouldGenerateCorrectCode()
    {
        string testCode = """
using MintyCore.Network;

namespace TestMod;

public partial class TestMessage : IMessage
{

}
""";

        string expectedCode = """
namespace TestMod;
    public partial class TestMessage
    {
        /// <summary>
        /// Send this message to the server
        /// </summary>
        public void SendToServer()
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.SendToServer(writer.ConstructBuffer(), DeliveryMethod);
            writer.Dispose();
        }
        
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(System.Collections.Generic.IEnumerable<ushort> receivers)
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receivers, writer.ConstructBuffer(), DeliveryMethod);
            writer.Dispose();
        }
        
        /// <summary>
        /// Send this message to the specified receiver
        /// </summary>
        public void Send(ushort receiver)
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receiver, writer.ConstructBuffer(), DeliveryMethod);
            writer.Dispose();
        }
        
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(ushort[] receivers)
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receivers, writer.ConstructBuffer(), DeliveryMethod);
            writer.Dispose();
        }
    }
""";

        Compile(testCode, out _, out var diagnostics, out var generatedTrees);

        Assert.Empty(diagnostics);
        Assert.Single(generatedTrees);
        Assert.True(CodeMatch(expectedCode, generatedTrees[0].ToString()));
    }

    [Fact]
    public void SendMessageGenerator_NoPartial_ShouldNotGenerateCode()
    {
        string testCode = """
using MintyCore.Network;

namespace TestMod;

public class TestMessage : IMessage
{

}
""";
        
        Compile(testCode, out _, out var diagnostics, out var generatedTrees);
        
        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void SendMessageGenerator_NestedClass_ShouldNotGenerateCode()
    {
        string testCode = """
using MintyCore.Network;

namespace TestMod;
public class Outer {
    public partial class TestMessage : IMessage
    {
        
    }
}
""";
        
        Compile(testCode, out _, out var diagnostics, out var generatedTrees);
        
        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }
}