using Microsoft.CodeAnalysis;
using System.Linq;

namespace MintyCoreGenerator.Registry;

public static class DiagnosticsHelper
{
    private static DiagnosticDescriptor _noRegisterMethodsDescriptor => new(
        DiagnosticIDs.NoRegisterMethods.ToIdString(),
        "No Register Methods found", "No Register Methods found for Registry {0}.", "MintyCoreGenerator",
        DiagnosticSeverity.Warning, true);

    public static Diagnostic NoRegisterMethods(Location? location, string className)
    {
        return Diagnostic.Create(_noRegisterMethodsDescriptor, location, className);
    }

    private static DiagnosticDescriptor _invalidRegisterMethodDescriptor => new(
        DiagnosticIDs.InvalidRegisterMethod.ToIdString(),
        "Invalid Register Method", "Invalid Register Method.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidRegisterMethod(Location? location, string methodName)
    {
        return Diagnostic.Create(_invalidRegisterMethodDescriptor, location, methodName);
    }

    private static DiagnosticDescriptor _invalidRegisterAttribute => new(
        DiagnosticIDs.InvalidRegisterAttribute.ToIdString(),
        "Invalid Register Attribute",
        "Register attribute {0} is invalid. Const field {1} is missing or the wrong type.",
        "MintyCoreGenerator", DiagnosticSeverity.Error, true);

    internal static Diagnostic InvalidRegisterAttribute(INamedTypeSymbol attributeClass, string v)
    {
        return Diagnostic.Create(_invalidRegisterAttribute, attributeClass.Locations.FirstOrDefault(),
            attributeClass.ToString(), v);
    }

    private static DiagnosticDescriptor _onlyOneModAllowed => new(
        DiagnosticIDs.OnlyOneModAllowed.ToIdString(),
        "Only one Mod Allowed", "Only one Mod implementation class per assembly allowed.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic OnlyOneModAllowed(Location first)
    {
        return Diagnostic.Create(_onlyOneModAllowed, first);
    }

    private static DiagnosticDescriptor _invalidGenericTypeForRegistry => new(
        DiagnosticIDs.InvalidGenericTypeForRegistry.ToIdString(),
        "Invalid Generic Type for Registry", "Generic type {0} is not usable for Registry.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidGenericTypeForRegistry(INamedTypeSymbol namedTypeSymbol)
    {
        return Diagnostic.Create(_invalidGenericTypeForRegistry, namedTypeSymbol.Locations.FirstOrDefault(),
            namedTypeSymbol.ToString());
    }

    private static DiagnosticDescriptor _invalidPropertyTypeForRegistry => new(
        DiagnosticIDs.InvalidPropertyTypeForRegistry.ToIdString(),
        "Invalid Property Type for Registry", "Type {0} is not usable for Registry.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidPropertyTypeForRegistry(IPropertySymbol namedTypeSymbol)
    {
        return Diagnostic.Create(_invalidPropertyTypeForRegistry, namedTypeSymbol.Locations.FirstOrDefault(),
            namedTypeSymbol.Type.ToString());
    }
}