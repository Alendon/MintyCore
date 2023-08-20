using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace MintyCore.Generator;

public static class Utils
{
    public static string ToCSharpString(this Accessibility accessibility)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return accessibility switch
        {
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "protected internal",
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null)
        };
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

    public static void CollectAllTypeSymbols(IAssemblySymbol assemblySymbol, HashSet<INamedTypeSymbol> typeSymbols,
        CancellationToken cancellationToken,
        Accessibility minimalAccessibility = Accessibility.Public)
    {
        foreach (var module in assemblySymbol.Modules)
        {
            CollectAllTypeSymbols(module, typeSymbols, cancellationToken, minimalAccessibility);
        }
    }

    public static void CollectAllTypeSymbols(IModuleSymbol moduleSymbol, HashSet<INamedTypeSymbol> typeSymbols,
        CancellationToken cancellationToken,
        Accessibility minimalAccessibility = Accessibility.Public)
    {
        foreach (var memberSymbol in moduleSymbol.GlobalNamespace.GetMembers())
        {
            CollectTypes(memberSymbol, typeSymbols, cancellationToken, minimalAccessibility);
        }
    }

    private static void CollectTypes(INamespaceOrTypeSymbol symbol, HashSet<INamedTypeSymbol> typeSymbols,
        CancellationToken cancellationToken, Accessibility minimalAccessibility)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (symbol)
        {
            case INamespaceSymbol namespaceSymbol:
            {
                foreach (var memberSymbol in namespaceSymbol.GetMembers())
                {
                    CollectTypes(memberSymbol, typeSymbols, cancellationToken, minimalAccessibility);
                }

                break;
            }
            case INamedTypeSymbol namedTypeSymbol when namedTypeSymbol.DeclaredAccessibility >= minimalAccessibility:
                typeSymbols.Add(namedTypeSymbol);
                break;
        }
    }
}