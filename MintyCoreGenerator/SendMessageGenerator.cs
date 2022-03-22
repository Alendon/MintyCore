using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MintyCoreGenerator
{
    [Generator]
    public class SendMessageGenerator : IIncrementalGenerator
    {
        private const string FullIMessageName = "MintyCore.Network.IMessage";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Initialize the generator
            //The predicate parameter is there to fast sort non matching syntax node
            //The transform parameter is for the actual sorting
            IncrementalValuesProvider<(ClassDeclarationSyntax, string)?> messageClassDeclarationProvider = context
                .SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (syntaxNode, _) => IsSyntaxTarget(syntaxNode),
                    transform: static (generatorSyntaxContext, _) =>
                        GetSemanticTargetForGeneration(generatorSyntaxContext))
                .Where(static m => m is not null);

            //Register the source output => the actual source generation for each syntax node
            context.RegisterSourceOutput(messageClassDeclarationProvider,
                static (spc, source) => Execute(source, spc));
        }


        private static bool IsSyntaxTarget(SyntaxNode node)
        {
            //Check if the syntax node is a non nested partial class
            //This method is used for a simple pre filtering.
            return node is ClassDeclarationSyntax classDeclaration &&
                   classDeclaration.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.PartialKeyword) &&
                   //A nested message class is not allowed
                   classDeclaration.Parent is BaseNamespaceDeclarationSyntax;
        }

        private static (ClassDeclarationSyntax messageClassDeclaration, string messageClassFullName)?
            GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            //Check if the syntax node implements the IMessage interface
            if (context.Node is not ClassDeclarationSyntax classDeclaration) return null;

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol is null)
            {
                //Should never happen
                return null;
            }

            //Check if the classDeclaration has the "IMessage" interface
            var interfaces = classSymbol.AllInterfaces;
            var found = false;

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            // No Linq execution as this would be much slower and more memory wasting
            foreach (var interfaceSymbol in interfaces)
            {
                if (!interfaceSymbol.ToString()!.Equals(FullIMessageName)) continue;
                found = true;
                break;
            }

            //All checks whether the class is a valid message class are done
            return found ? (classDeclaration, classSymbol.ToString()!) : null;
        }

        private static void Execute((ClassDeclarationSyntax, string)? classTuple, SourceProductionContext context)
        {
            //Add the generated class text
            if (classTuple is null) return;
            var (classDeclaration, fullClassName) = classTuple.Value;
            
            //Could be removed, is just there to apply correct formatting to the generated source code
            CompilationUnitSyntax compileSyntax = SyntaxFactory
                .ParseCompilationUnit(GetClassText(fullClassName, classDeclaration.Modifiers))
                .NormalizeWhitespace();
            context.AddSource($"{fullClassName}.g.cs", compileSyntax.ToFullString());
        }

        private static string GetClassText(string fullClassName, SyntaxTokenList modifiers)
        {
            //"Compile" the class text
            
            int lastDotIndex = fullClassName.LastIndexOf('.');
            //If the class is not in any namespace the index of the last dot will be -1
            //If the class is in a namespace the following values will be overriden
            string namespaceName = String.Empty;
            string className = fullClassName;
            if (lastDotIndex >= 0)
            {
                //"Split" the fullClassName to the namespace name and class name
                namespaceName = fullClassName.Substring(0, lastDotIndex);
                className = fullClassName.Substring(lastDotIndex + 1);
            }

            //The only modifiers we interested in are the access modifiers public and internal
            string accessor = String.Empty;
            foreach (var modifier in modifiers)
            {
                accessor = modifier.Kind() switch
                {
                    SyntaxKind.PublicKeyword => "public",
                    SyntaxKind.InternalKeyword => "internal",
                    _ => accessor
                };
                if (accessor.Length != 0) break;
            }

            return $@"{(namespaceName.Length != 0 ? $"namespace {namespaceName};" : String.Empty)}
    {accessor} partial class {className}
    {{
        /// <summary>
        /// Send this message to the server
        /// </summary>
        public void SendToServer()
        {{
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.SendToServer(writer.ConstructBuffer(), writer.Length, DeliveryMethod);
        }}
        
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(System.Collections.Generic.IEnumerable<ushort> receivers)
        {{
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receivers, writer.ConstructBuffer(), writer.Length, DeliveryMethod);
        }}
        
        /// <summary>
        /// Send this message to the specified receiver
        /// </summary>
        public void Send(ushort receiver)
        {{
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receiver, writer.ConstructBuffer(), writer.Length, DeliveryMethod);
        }}
        
        /// <summary>
        /// Send this message to the specified receivers
        /// </summary>
        public void Send(ushort[] receivers)
        {{
            var writer = new MintyCore.Utils.DataWriter();
            writer.Put(ReceiveMultiThreaded);
            MessageId.Serialize(writer);
            Serialize(writer);
            MintyCore.Network.NetworkHandler.Send(receivers, writer.ConstructBuffer(), writer.Length, DeliveryMethod);
        }}
    }}";
        }
    }
}