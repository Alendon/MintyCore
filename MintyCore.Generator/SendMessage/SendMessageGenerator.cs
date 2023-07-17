using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace MintyCoreGenerator.SendMessage;

[Generator]
public class SendMessageGenerator : IIncrementalGenerator
{
    

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Initialize the generator
        //The predicate parameter is there to fast sort non matching syntax node
        //The transform parameter is for the actual sorting
        IncrementalValuesProvider<INamedTypeSymbol?> messageClassDeclarationProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (syntaxNode, _) => IsSyntaxTarget(syntaxNode),
                transform: (generatorSyntaxContext, _) =>
                    GetSemanticTargetForGeneration(generatorSyntaxContext))
            .Where(static m => m is not null);

        //Register the source output => the actual source generation for each syntax node
        context.RegisterSourceOutput(messageClassDeclarationProvider,
            (spc, source) => Execute(source, spc));
    }


    private bool IsSyntaxTarget(SyntaxNode node)
    {
        //Check if the syntax node is a non nested partial class
        //This method is used for a simple pre filtering.
        return node is ClassDeclarationSyntax classDeclaration &&
               classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
               //A nested message class is not allowed
               classDeclaration.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax;
    }

    private INamedTypeSymbol?
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
        
        return interfaces.Any(x => x.ToString() == Constants.FullIMessageName) ? classSymbol : null;
    }

    private void Execute(INamedTypeSymbol messageClass, SourceProductionContext context)
    {
        using var templateStream = GetType().Assembly.GetManifestResourceStream("MintyCoreGenerator.SendMessage.SendMessageTemplate.sbncs");
        using var reader = new StreamReader(templateStream);
        var template = Template.Parse(reader.ReadToEnd());

        var accessor = messageClass.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            _ => throw new ApplicationException("Invalid accessibility")
        };
        string? @namespace = null;
        
        if(messageClass.ContainingNamespace is not null)
            @namespace = messageClass.ContainingNamespace.ToString();
        
        var className = messageClass.Name;
        
        var scriptObject = new ScriptObject
        {
            { "Accessor", accessor },
            { "Namespace", @namespace },
            { "ClassName", className }
        };
        
        var templateContext = new TemplateContext(scriptObject);
        
        var result = template.Render(templateContext);

        //Could be removed, is just there to apply correct formatting to the generated source code
        var compileSyntax = SyntaxFactory
            .ParseCompilationUnit(result)
            .NormalizeWhitespace();
        
        var fullClassName = @namespace is not null ? $"{@namespace}.{className}" : className;
        
        context.AddSource($"{fullClassName}.g.cs", compileSyntax.ToFullString());
    }
}