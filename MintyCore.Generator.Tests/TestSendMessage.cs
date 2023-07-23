using Microsoft.CodeAnalysis.CSharp;
using MintyCore.Generator.SendMessage;
using static MintyCore.Generator.Tests.SourceGenHelper;

namespace MintyCore.Generator.Tests;

public class TestSendMessage
{
    
    
    
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
            var writer = new global::MintyCore.Utils.DataWriter();
            writer.Put(this.ReceiveMultiThreaded);
            this.MessageId.Serialize(writer);
            this.Serialize(writer);
            global::MintyCore.Network.NetworkHandler.SendToServer(writer.ConstructBuffer(), this.DeliveryMethod);
            writer.Dispose();
        }
    
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(System.Collections.Generic.IEnumerable<ushort> receivers)
        {
            var writer = new global::MintyCore.Utils.DataWriter();
            writer.Put(this.ReceiveMultiThreaded);
            this.MessageId.Serialize(writer);
            this.Serialize(writer);
            global::MintyCore.Network.NetworkHandler.Send(receivers, writer.ConstructBuffer(), this.DeliveryMethod);
            writer.Dispose();
        }
    
        /// <summary>
        /// Send this message to the specified receiver
        /// </summary>
        public void Send(ushort receiver)
        {
            var writer = new global::MintyCore.Utils.DataWriter();
            writer.Put(this.ReceiveMultiThreaded);
            this.MessageId.Serialize(writer);
            this.Serialize(writer);
            global::MintyCore.Network.NetworkHandler.Send(receiver, writer.ConstructBuffer(), this.DeliveryMethod);
            writer.Dispose();
        }
    
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(ushort[] receivers)
        {
            var writer = new global::MintyCore.Utils.DataWriter();
            writer.Put(this.ReceiveMultiThreaded);
            this.MessageId.Serialize(writer);
            this.Serialize(writer);
            global::MintyCore.Network.NetworkHandler.Send(receivers, writer.ConstructBuffer(), this.DeliveryMethod);
            writer.Dispose();
        }
    }
""";
        
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
        string testCode = """
using MintyCore.Network;

namespace TestMod;

public class TestMessage : IMessage
{

}
""";
        
        Compile(new SendMessageGenerator(),  out _, out var diagnostics, out var generatedTrees,
            testCode, MessageInterface);
        
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
        
        Compile(new SendMessageGenerator(),  out _, out var diagnostics, out var generatedTrees,
            testCode, MessageInterface);
        
        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void SendMessageAnalyzer_ShouldReportNoError()
    {
        string testCode = """
using MintyCore.Network;

namespace TestMod;

public partial class TestMessage : IMessage
{

}
""";
        
        Analyze( new SendMessageAnalyzer(), out var diagnostics,
            testCode, MessageInterface);
        
        Assert.Empty(diagnostics);
    }
    
    [Fact]
    public void SendMessageAnalyzer_NoPartial_ShouldReportWarning()
    {
        string testCode = """
using MintyCore.Network;

namespace TestMod;

public class TestMessage : IMessage
{

}
""";
        
        Analyze( new SendMessageAnalyzer(), out var diagnostics,
            testCode, MessageInterface);
        
        Assert.Single(diagnostics);
        Assert.Equal("MC3102", diagnostics[0].Id);
    }
    
    [Fact]
    public void SendMessageAnalyzer_Nested_ShouldReportWarning()
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
        
        Analyze( new SendMessageAnalyzer(), out var diagnostics,
            testCode, MessageInterface);
        
        Assert.Single(diagnostics);
        Assert.Equal("MC3101", diagnostics[0].Id);
    }
}