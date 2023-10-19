using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MintyCore.Generator.LogEnricher;

public class LogEnricherGenerator : IIncrementalGenerator
{
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
        var logInvocationProvider = context.SyntaxProvider.CreateSyntaxProvider<(IMethodSymbol, InvocationExpressionSyntax)?>(
            (node, _) => node is InvocationExpressionSyntax,
            (IMethodSymbol, InvocationExpressionSyntax)? (ctx, _) =>
            {
                if (ctx.Node is not InvocationExpressionSyntax invocationExpressionSyntax)
                    return null;
                var methodSymbol = ctx.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol as IMethodSymbol;
                if (methodSymbol is null)
                    return null;

                return IsValidLogMethod(methodSymbol) ? (methodSymbol, invocationExpressionSyntax) : null;
            }
        ).Where<(IMethodSymbol, InvocationExpressionSyntax)?>(x => x is not null).Select((x, _) => x!.Value);
        
        context.RegisterSourceOutput(logInvocationProvider, GenerateLogInterceptor);
    }

    private void GenerateLogInterceptor(SourceProductionContext ctx, (IMethodSymbol, InvocationExpressionSyntax) arg2)
    {
        
    }

    private bool IsValidLogMethod(IMethodSymbol methodSymbol)
    {
        if(!Array.Exists(LogMethods, x => x == methodSymbol.Name))
            return false;

        var methodTypeName = methodSymbol.OriginalDefinition.ContainingType?.ToDisplayString();
        return Array.Exists(LoggerTypes, x => x == methodTypeName);
    }
}