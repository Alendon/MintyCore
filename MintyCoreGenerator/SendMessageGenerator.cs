using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MintyCoreGenerator
{
    [Generator]
    public class SendMessageGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();
            var messages = GetMessages(context);

            foreach (var message in messages)
            {
                var (source, name) = GetExtensionCode(message);
                context.AddSource(name, source);
            }
        }

        private const string genericClassText = @"namespace {namespace}
{
    {accessor} partial class {className}
    {
        /// <summary>
        /// Send this message to the server
        /// </summary>
        public void SendToServer()
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put((int)MessageType.REGISTERED_MESSAGE);
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.SendToServer(writer.Buffer, writer.Length, DeliveryMethod);
        }
        
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(System.Collections.Generic.IEnumerable<ushort> receivers)
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put((int)MessageType.REGISTERED_MESSAGE);
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receivers, writer.Buffer, writer.Length, DeliveryMethod);
        }
        
        /// <summary>
        /// Send this message to the specified receiver
        /// </summary>
        public void Send(ushort receiver)
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put((int)MessageType.REGISTERED_MESSAGE);
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receiver, writer.Buffer, writer.Length, DeliveryMethod);
        }
        
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(ushort[] receivers)
        {
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put((int)MessageType.REGISTERED_MESSAGE);
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receivers, writer.Buffer, writer.Length, DeliveryMethod);
        }
    }
}";

        private (string source, string name) GetExtensionCode(ClassDeclarationSyntax message)
        {
            var namespaceDeclaration = message.Parent as BaseNamespaceDeclarationSyntax;
            string accessor = String.Empty;
            if (message.Modifiers.Any(x => x.Text.Equals("public"))) accessor = "public";
            if (message.Modifiers.Any(x => x.Text.Equals("internal"))) accessor = "internal";

            string classText = genericClassText
                .Replace("{namespace}", namespaceDeclaration.Name.ToString())
                .Replace("{className}", message.Identifier.ValueText)
                .Replace("{accessor}", accessor);
            
            
            
            return (classText, $"{namespaceDeclaration.Name.ToString()}_{message.Identifier.ValueText}_ext.cs");
        }

        public IEnumerable<ClassDeclarationSyntax> GetMessages(
            GeneratorExecutionContext context)
        {
            var iMessageType = SyntaxFactory.Identifier("IMessage");

            var IMessageTypes = from syntaxTree in context.Compilation.SyntaxTrees
                from classDeclaration in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                where classDeclaration.BaseList != null &&
                      classDeclaration.BaseList.Types.Any(x =>
                          ((SimpleNameSyntax)x.Type).Identifier.Text.Equals("IMessage")) &&
                      classDeclaration.Modifiers.Any(x => x.Text.Equals("partial")) &&
                      classDeclaration.Parent.Parent is CompilationUnitSyntax
                select classDeclaration;

            return IMessageTypes;
        }
    }
}