using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MintyCore.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WorldImplementationAnalyzer : DiagnosticAnalyzer
{
    private const string WorldInterfaceName = "MintyCore.ECS.IWorld";
    private const string WorldRegisterAttribute = "MintyCore.Registries.RegisterWorldAttribute";

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeWorldConstructor, SymbolKind.NamedType);
    }

    private void AnalyzeWorldConstructor(SymbolAnalysisContext obj)
    {
        if (obj.Symbol is not INamedTypeSymbol { Kind: SymbolKind.NamedType } classSymbol)
            return;

        if (!classSymbol.Interfaces.Any(i => i.ToDisplayString() == WorldInterfaceName))
            return;

        //check if the class has the RegisterWorldAttribute
        if (!classSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == WorldRegisterAttribute))
            return;

        var constructors = classSymbol.Constructors;

        foreach (var constructor in constructors)
        {
            if (constructor.Parameters.Any(p =>
                {
                    var isBool = p.Type.SpecialType == SpecialType.System_Boolean;
                    var isServerWorld = p.Name == "isServerWorld";

                    return isBool && isServerWorld;
                })) continue;
            
            obj.ReportDiagnostic(DiagnosticsHelper.WorldConstructorNeedsIsServerWorldDiagnostic(constructor));
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticsHelper.WorldConstructorNeedsIsServerWorld
    );
}