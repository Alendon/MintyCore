using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MintyCore.Generator;

public static class GenericHelper
{
    public static (GenericConstraints constraints, string[] constraintTypes) GetGenericConstraint(
        ITypeParameterSymbol typeSymbol)
    {
        var constraints = GenericConstraints.None;

        constraints |= typeSymbol.HasReferenceTypeConstraint
            ? GenericConstraints.ReferenceType
            : GenericConstraints.None;
        constraints |= typeSymbol.HasValueTypeConstraint ? GenericConstraints.ValueType : GenericConstraints.None;
        constraints |= typeSymbol.HasConstructorConstraint ? GenericConstraints.Constructor : GenericConstraints.None;
        constraints |= typeSymbol.HasUnmanagedTypeConstraint
            ? GenericConstraints.UnmanagedType
            : GenericConstraints.None;
        constraints |= typeSymbol.HasNotNullConstraint ? GenericConstraints.NotNull : GenericConstraints.None;

        return (constraints, typeSymbol.ConstraintTypes.Select(type => type.ToString()).ToArray());
    }

    public static bool CheckValidConstraint(GenericConstraints? genericConstraints, string[]? genericConstraintTypes,
        INamedTypeSymbol namedTypeSymbol)
    {
        if (genericConstraints is null || genericConstraintTypes is null)
        {
            return true;
        }

        var constraints = genericConstraints.Value;
        var constraintTypes = genericConstraintTypes;

        if (constraints.HasFlag(GenericConstraints.ReferenceType) && !namedTypeSymbol.IsReferenceType)
        {
            return false;
        }

        if (constraints.HasFlag(GenericConstraints.ValueType) && !namedTypeSymbol.IsValueType)
        {
            return false;
        }

        if (constraints.HasFlag(GenericConstraints.UnmanagedType) && !namedTypeSymbol.IsUnmanagedType)
        {
            return false;
        }

        //check if al generic constraint types are present
        // ReSharper disable once InvertIf
        if (constraintTypes.Length > 0)
        {
            var baseTypesEnum = GetBaseTypes(namedTypeSymbol);
            var namedTypeSymbols = baseTypesEnum as INamedTypeSymbol[] ?? baseTypesEnum.ToArray();
            var interfaces = namedTypeSymbol.AllInterfaces;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var constraintType in constraintTypes)
            {
                var interfaceFound = interfaces.Any(@interface => @interface.ToString().Equals(constraintType));
                var baseFound = Array.Exists(namedTypeSymbols,type => type.ToString().Equals(constraintType));

                var found = interfaceFound || baseFound;
                if (!found)
                    return false;
            }
        }

        return true;
    }

    private static IEnumerable<INamedTypeSymbol> GetBaseTypes(ITypeSymbol symbol)
    {
        var current = symbol.BaseType;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}

[Flags]
public enum GenericConstraints
{
    None = 0,
    Constructor = 1 << 0,
    NotNull = 1 << 1,
    ReferenceType = 1 << 2,
    UnmanagedType = 1 << 3,
    ValueType = 1 << 4
}