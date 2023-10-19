using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using SharedCode;

namespace MintyCore.Generator.LogEnricher;

public class LogEnricherGenerator : IIncrementalGenerator
{
    private const string TemplateDirectory = "MintyCore.Generator.LogEnricher";

    private static Template LogInterceptorTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.LogInterceptors.sbncs"));

    private static readonly string[] LoggerTypes =
    {
        "Serilog.Log",
        "Serilog.ILogger"
    };

    private static readonly string[] LogMethods =
    {
        "Write",
        "Verbose",
        "Debug",
        "Information",
        "Warning",
        "Error",
        "Fatal"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //collect all log invocations
        var logInvocationProvider = context.SyntaxProvider
            .CreateSyntaxProvider<(INamedTypeSymbol?, IMethodSymbol, InvocationExpressionSyntax)?>(
                (node, _) => node is InvocationExpressionSyntax,
                (INamedTypeSymbol?, IMethodSymbol, InvocationExpressionSyntax)? (ctx, _) =>
                {
                    if (ctx.Node is not InvocationExpressionSyntax invocationExpressionSyntax)
                        return null;
                    var containingClassSyntax =
                        invocationExpressionSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                    var containingClassSymbol = containingClassSyntax is not null
                        ? ctx.SemanticModel.GetDeclaredSymbol(containingClassSyntax) as INamedTypeSymbol
                        : null;

                    if (ctx.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol is not IMethodSymbol
                        methodSymbol)
                        return null;

                    methodSymbol = methodSymbol.ConstructedFrom;

                    return IsValidLogMethod(methodSymbol)
                        ? (containingClassSymbol, methodSymbol, invocationExpressionSyntax)
                        : null;
                }
            ).Where<(INamedTypeSymbol?, IMethodSymbol, InvocationExpressionSyntax)?>(x => x is not null)
            .Select((x, _) => x!.Value);

        var byClass = logInvocationProvider.Collect()
            .SelectMany((methodInvocations, _) =>
                methodInvocations.GroupBy(tuple => tuple.Item1, SymbolEqualityComparer.Default));

        context.RegisterSourceOutput(byClass, GenerateLogInterceptor);
    }

    private void GenerateLogInterceptor(SourceProductionContext ctx,
        IGrouping<ISymbol?, (INamedTypeSymbol?, IMethodSymbol, InvocationExpressionSyntax)> grouping)
    {
        if (grouping.Key is not INamedTypeSymbol classSymbol)
            return;

        var description = new LogMethodsDescription
        {
            RootNamespace = classSymbol.ContainingNamespace.ToDisplayString(),
            Class = classSymbol.Name,
            LogMethods = new List<LogMethodEntry>()
        };

        var logs = grouping.ToArray();

        foreach (var (_, logMethod, logInvocation) in logs)
        {
            var methodEntry = new LogMethodEntry
            {
                MethodName = logMethod.Name,
                StaticLogger = logMethod.IsStatic,
                FileLocation = logInvocation.GetLocation().GetLineSpan().Path,
                LineNumber = (logInvocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1).ToString(),
                CharacterNumber =
                    (logInvocation.GetLocation().GetLineSpan().StartLinePosition.Character + 1).ToString(),
                GenericParameters = logMethod.TypeParameters.Select(x => x.ToDisplayString()).ToList(),
            };

            foreach (var parameterSymbol in logMethod.Parameters)
            {
                var parameterStrings = parameterSymbol.ToDisplayString(new SymbolDisplayFormat(
                    SymbolDisplayGlobalNamespaceStyle.Included,
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    parameterOptions: SymbolDisplayParameterOptions.IncludeType)).Split(' ');


                methodEntry.Parameters.Add(new LogMethodParameters()
                {
                    Type = parameterStrings[0],
                    Name = parameterStrings[1]
                });
            }

            description.LogMethods.Add(methodEntry);
        }

        var result = LogInterceptorTemplate.Render(description, member => member.Name);
        ctx.AddSource($"{classSymbol.Name}.LogInterceptor.cs", result);
    }

    private bool IsValidLogMethod(IMethodSymbol methodSymbol)
    {
        if (!Array.Exists(LogMethods, x => x == methodSymbol.Name))
            return false;

        var methodTypeName = methodSymbol.OriginalDefinition.ContainingType?.ToDisplayString();
        return Array.Exists(LoggerTypes, x => x == methodTypeName);
    }

    class LogMethodsDescription
    {
        public string RootNamespace { get; set; }
        public string Class { get; set; }
        public List<LogMethodEntry> LogMethods { get; set; }
    }

    class LogMethodEntry
    {
        public List<string> GenericParameters { get; set; } = new();
        public bool StaticLogger { get; set; }
        public List<LogMethodParameters> Parameters { get; set; } = new();
        public string MethodName { get; set; }

        public string FileLocation { get; set; }
        public string LineNumber { get; set; }
        public string CharacterNumber { get; set; }
    }

    class LogMethodParameters
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}