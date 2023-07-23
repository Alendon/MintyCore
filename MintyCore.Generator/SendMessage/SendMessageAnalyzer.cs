using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MintyCore.Generator.SendMessage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SendMessageAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        if (!Debugger.IsAttached)
            context.EnableConcurrentExecution();

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(CheckNested, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(CheckPartial, SyntaxKind.ClassDeclaration);
    }

    private void CheckPartial(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
            return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol is null)
            return;

        if (!classSymbol.Interfaces.Any(i => i.ToString() == Constants.FullIMessageName))
            return;
        
        if (!classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(DiagnosticsHelper.MessageNotPartialDiagnostic(classSymbol));
        }
    }

    private void CheckNested(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
            return;

        //check if the symbol is a class
        if (namedTypeSymbol is not { TypeKind: TypeKind.Class })
            return;

        //check if the class implements the IMessage interface
        if (!namedTypeSymbol.Interfaces.Any(i => i.ToString() == Constants.FullIMessageName))
            return;

        //check if the class is nested
        if (namedTypeSymbol.ContainingType is not null)
        {
            context.ReportDiagnostic(DiagnosticsHelper.MessageNestedDiagnostic(namedTypeSymbol));
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticsHelper.MessageNested, DiagnosticsHelper.MessageNotPartial
    );
}