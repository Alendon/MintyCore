using System.Linq;
using Microsoft.CodeAnalysis;

namespace MintyCoreGenerator;

public static class DiagnosticsHelper
{
    private static DiagnosticDescriptor NoRegisterMethodsDescriptor => new(
        DiagnosticIDs.NoRegisterMethods.ToIdString(),
        "No Register Methods found", "No Register Methods found for Registry {0}.", "MintyCoreGenerator",
        DiagnosticSeverity.Warning, true);

    public static Diagnostic NoRegisterMethods(Location? location, string className)
    {
        return Diagnostic.Create(NoRegisterMethodsDescriptor, location, className);
    }

    private static DiagnosticDescriptor InvalidRegisterMethodDescriptor => new(
        DiagnosticIDs.InvalidRegisterMethod.ToIdString(),
        "Invalid Register Method", "Invalid Register Method.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidRegisterMethod(Location? location, string methodName)
    {
        return Diagnostic.Create(InvalidRegisterMethodDescriptor, location, methodName);
    }

    private static DiagnosticDescriptor InvalidRegisterAttributeDescriptor => new(
        DiagnosticIDs.InvalidRegisterAttribute.ToIdString(),
        "Invalid Register Attribute",
        "Register attribute {0} is invalid. Const field {1} is missing or the wrong type.",
        "MintyCoreGenerator", DiagnosticSeverity.Error, true);

    internal static Diagnostic InvalidRegisterAttribute(INamedTypeSymbol attributeClass, string v)
    {
        return Diagnostic.Create(InvalidRegisterAttributeDescriptor, attributeClass.Locations.FirstOrDefault(),
            attributeClass.ToString(), v);
    }

    private static DiagnosticDescriptor OnlyOneModAllowedDescriptor => new(
        DiagnosticIDs.OnlyOneModAllowed.ToIdString(),
        "Only one Mod Allowed", "Only one Mod implementation class per assembly allowed.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic OnlyOneModAllowed(Location first)
    {
        return Diagnostic.Create(OnlyOneModAllowedDescriptor, first);
    }

    private static DiagnosticDescriptor InvalidGenericTypeForRegistryDescriptor => new(
        DiagnosticIDs.InvalidGenericTypeForRegistry.ToIdString(),
        "Invalid Generic Type for Registry", "Generic type {0} is not usable for Registry.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidGenericTypeForRegistry(INamedTypeSymbol namedTypeSymbol)
    {
        return Diagnostic.Create(InvalidGenericTypeForRegistryDescriptor, namedTypeSymbol.Locations.FirstOrDefault(),
            namedTypeSymbol.ToString());
    }

    private static DiagnosticDescriptor InvalidPropertyTypeForRegistryDescriptor => new(
        DiagnosticIDs.InvalidPropertyTypeForRegistry.ToIdString(),
        "Invalid Property Type for Registry", "Type {0} is not usable for Registry.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);

    public static Diagnostic InvalidPropertyTypeForRegistry(IPropertySymbol namedTypeSymbol)
    {
        return Diagnostic.Create(InvalidPropertyTypeForRegistryDescriptor, namedTypeSymbol.Locations.FirstOrDefault(),
            namedTypeSymbol.Type.ToString());
    }
}