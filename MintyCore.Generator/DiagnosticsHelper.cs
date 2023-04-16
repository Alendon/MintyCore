using System.Linq;
using Microsoft.CodeAnalysis;

namespace MintyCoreGenerator;

public static class DiagnosticsHelper
{
    private static readonly DiagnosticDescriptor _noRegisterMethodsDescriptor = new(
        DiagnosticIDs.NoRegisterMethods.ToIdString(),
        "No Register Methods found", "No Register Methods found for Registry {0}.", "MintyCore.Generator",
        DiagnosticSeverity.Warning, true);

    public static Diagnostic NoRegisterMethods(Location? location, string className)
    {
        return Diagnostic.Create(_noRegisterMethodsDescriptor, location, className);
    }

    private static readonly DiagnosticDescriptor _invalidRegisterMethodDescriptor = new(
        DiagnosticIDs.InvalidRegisterMethod.ToIdString(),
        "Invalid Register Method", "Invalid Register Method: '{0}'; '{1}' {2}.", "MintyCore.Generator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidRegisterMethod(Location? location, string methodName, string? reason)
    {
        return Diagnostic.Create(_invalidRegisterMethodDescriptor, location, methodName, reason, "Hello World!!!");
    }

    private static readonly DiagnosticDescriptor _invalidRegisterAttributeDescriptor = new(
        DiagnosticIDs.InvalidRegisterAttribute.ToIdString(),
        "Invalid Register Attribute",
        "Register attribute {0} is invalid. Const field {1} is missing or the wrong type.",
        "MintyCore.Generator", DiagnosticSeverity.Error, true);

    internal static Diagnostic InvalidRegisterAttribute(INamedTypeSymbol attributeClass, string v)
    {
        return Diagnostic.Create(_invalidRegisterAttributeDescriptor, attributeClass.Locations.FirstOrDefault(),
            attributeClass.ToString(), v);
    }

    private static readonly DiagnosticDescriptor _onlyOneModAllowedDescriptor = new(
        DiagnosticIDs.OnlyOneModAllowed.ToIdString(),
        "Only one Mod Allowed", "Only one Mod implementation class per assembly allowed.", "MintyCore.Generator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic OnlyOneModAllowed(Location first)
    {
        return Diagnostic.Create(_onlyOneModAllowedDescriptor, first);
    }

    private static readonly DiagnosticDescriptor _invalidGenericTypeForRegistryDescriptor = new(
        DiagnosticIDs.InvalidGenericTypeForRegistry.ToIdString(),
        "Invalid Generic Type for Registry", "Generic type {0} is not usable for Registry.", "MintyCore.Generator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidGenericTypeForRegistry(INamedTypeSymbol namedTypeSymbol)
    {
        return Diagnostic.Create(_invalidGenericTypeForRegistryDescriptor, namedTypeSymbol.Locations.FirstOrDefault(),
            namedTypeSymbol.ToString());
    }

    private static readonly DiagnosticDescriptor _invalidPropertyTypeForRegistryDescriptor = new(
        DiagnosticIDs.InvalidPropertyTypeForRegistry.ToIdString(),
        "Invalid Property Type for Registry", "Type {0} is not usable for Registry.", "MintyCore.Generator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidPropertyTypeForRegistry(IPropertySymbol namedTypeSymbol)
    {
        return Diagnostic.Create(_invalidPropertyTypeForRegistryDescriptor, namedTypeSymbol.Locations.FirstOrDefault(),
            namedTypeSymbol.Type.ToString());
    }
}