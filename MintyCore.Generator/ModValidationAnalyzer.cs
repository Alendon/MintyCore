using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MintyCore.Generator;

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

#pragma warning disable RS1013
        context.RegisterCompilationStartAction(compilationStart =>
#pragma warning restore RS1013
        {
            compilationStart.RegisterCompilationEndAction(c =>
            {
                if (_modsFound == 0)
                    c.ReportDiagnostic(DiagnosticsHelper.NeedOneModInAssemblyDiagnostic());
            });
        });
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
    
    public static bool IsModClass(ISymbol symbol, out INamedTypeSymbol? namedTypeSymbol)
    {
        namedTypeSymbol = null;

        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            return false;
        }

        namedTypeSymbol = typeSymbol;

        if (namedTypeSymbol.TypeKind != TypeKind.Class)
            return false;

        var interfaces = namedTypeSymbol.Interfaces;
        return interfaces.Length != 0 && interfaces.Any(i => i.ToString().Equals(Constants.ModInterface));
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticsHelper.OnlyOneModPerAssembly, DiagnosticsHelper.NeedOneModInAssembly,
            DiagnosticsHelper.PublicModClass, DiagnosticsHelper.SealedModClass);
}