using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static MintyCoreGenerator.Utils;

namespace MintyCoreGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ModValidationAnalyzer : DiagnosticAnalyzer
{
    private int _modsFound;

    public override void Initialize(AnalysisContext context)
    {
        _modsFound = 0;

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        //Easy workaround for debugging. Debugging concurrent analyzers is a pain.
        if (!Debugger.IsAttached)
            context.EnableConcurrentExecution();

        context.RegisterSymbolAction(CheckDuplicateMod, SymbolKind.NamedType);
        context.RegisterSymbolAction(CheckPublicSealed, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(CheckPartialClass, SyntaxKind.ClassDeclaration);


        context.RegisterCompilationAction(c =>
        {
            if (_modsFound == 0)
                c.ReportDiagnostic(DiagnosticsHelper.NeedOneModInAssemblyDiagnostic());
        });
    }

    private void CheckPartialClass(SyntaxNodeAnalysisContext obj)
    {
        if (obj.Node is not ClassDeclarationSyntax classNode) return;

        var classSymbol = obj.SemanticModel.GetDeclaredSymbol(classNode);
        if (classSymbol is null || !IsModClass(classSymbol, out var modSymbol)) return;

        if (!classNode.Modifiers.Any(SyntaxKind.PartialKeyword))
            obj.ReportDiagnostic(DiagnosticsHelper.PartialModClassDiagnostic(modSymbol!));
    }

    private static void CheckPublicSealed(SymbolAnalysisContext obj)
    {
        if (!IsModClass(obj.Symbol, out var modSymbol)) return;

        if (modSymbol!.DeclaredAccessibility != Accessibility.Public)
            obj.ReportDiagnostic(DiagnosticsHelper.PublicModClassDiagnostic(modSymbol));

        if (!modSymbol.IsSealed)
            obj.ReportDiagnostic(DiagnosticsHelper.SealedModClassDiagnostic(modSymbol));
    }


    private void CheckDuplicateMod(SymbolAnalysisContext obj)
    {
        if (!IsModClass(obj.Symbol, out var modSymbol)) return;

        if (Interlocked.Increment(ref _modsFound) > 1)
            obj.ReportDiagnostic(DiagnosticsHelper.OnlyOneModPerAssemblyDiagnostic(modSymbol!));
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticsHelper.OnlyOneModPerAssembly, DiagnosticsHelper.NeedOneModInAssembly,
            DiagnosticsHelper.PublicModClass, DiagnosticsHelper.SealedModClass, DiagnosticsHelper.PartialModClass);
}