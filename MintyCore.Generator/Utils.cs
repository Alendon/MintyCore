using System;
using System.Linq;
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
}