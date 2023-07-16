using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
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
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(CheckDuplicateMod, SymbolKind.NamedType);
        
        
        context.RegisterCompilationAction(c =>
        {
            if (_modsFound == 0)
                c.ReportDiagnostic(DiagnosticsHelper.NeedOneModInAssemblyDiagnostic());
        });
    }


    private void CheckDuplicateMod(SymbolAnalysisContext obj)
    {
        if (!IsModClass(obj.Symbol, out var modSymbol)) return;

        if (Interlocked.Increment(ref _modsFound) > 1)
            obj.ReportDiagnostic(DiagnosticsHelper.OnlyOneModPerAssemblyDiagnostic(modSymbol!));
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticsHelper.OnlyOneModPerAssembly, DiagnosticsHelper.NeedOneModInAssembly);
}