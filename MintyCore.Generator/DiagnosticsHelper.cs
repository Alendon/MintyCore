using System.Linq;
using Microsoft.CodeAnalysis;

namespace MintyCore.Generator;

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
    
    public static readonly DiagnosticDescriptor OnlyOneModPerAssembly = new(
        DiagnosticIDs.OnlyOneModPerAssembly.ToIdString(),
        "Only one Mod Allowed", "Only one Mod implementation class per assembly allowed.", "MintyCore.Generator",
        DiagnosticSeverity.Error, true);
    
    public static Diagnostic OnlyOneModPerAssemblyDiagnostic(INamedTypeSymbol modType)
    {
        return Diagnostic.Create(OnlyOneModPerAssembly, modType.Locations.FirstOrDefault());
    }
    
    public static readonly DiagnosticDescriptor NeedOneModInAssembly = new(
        DiagnosticIDs.NeedOneModInAssembly.ToIdString(),
        "Need one Mod", "Need one Mod implementation class per assembly.", "MintyCore.Generator",
        DiagnosticSeverity.Error, true);
    
    public static Diagnostic NeedOneModInAssemblyDiagnostic()
    {
        return Diagnostic.Create(NeedOneModInAssembly, null);
    }
    
    public static readonly DiagnosticDescriptor PublicModClass = new(
        DiagnosticIDs.PublicModClass.ToIdString(),
        "Mod class should be public", "Mod class {0} should be public.", "MintyCore.Generator",
        DiagnosticSeverity.Warning, true);

    public static Diagnostic PublicModClassDiagnostic(INamedTypeSymbol modType)
    {
        return Diagnostic.Create(PublicModClass, modType.Locations.FirstOrDefault(), modType.ToString());
    }
    
    public static readonly DiagnosticDescriptor SealedModClass = new(
        DiagnosticIDs.SealedModClass.ToIdString(),
        "Mod class should be sealed", "Mod class {0} should be sealed.", "MintyCore.Generator",
        DiagnosticSeverity.Warning, true);
    
    public static Diagnostic SealedModClassDiagnostic(INamedTypeSymbol modType)
    {
        return Diagnostic.Create(SealedModClass, modType.Locations.FirstOrDefault(), modType.ToString());
    }
    
    public static readonly DiagnosticDescriptor PartialModClass = new(
        DiagnosticIDs.PartialModClass.ToIdString(),
        "Mod class should be partial", "Mod class {0} should be partial to enable source generated extensions.", "MintyCore.Generator",
        DiagnosticSeverity.Warning, true);
    
    public static Diagnostic PartialModClassDiagnostic(INamedTypeSymbol modType)
    {
        return Diagnostic.Create(PartialModClass, modType.Locations.FirstOrDefault(), modType.ToString());
    }
    
    public static readonly DiagnosticDescriptor MessageNested = new(
        DiagnosticIDs.MessageNested.ToIdString(),
        "Message class should not be nested", "Message class {0} should not be nested to enable source generated extensions.", "MintyCore.Generator",
        DiagnosticSeverity.Warning, true);
    
    public static Diagnostic MessageNestedDiagnostic(INamedTypeSymbol messageType)
    {
        return Diagnostic.Create(MessageNested, messageType.Locations.FirstOrDefault(), messageType.ToString());
    }
    
    public static readonly DiagnosticDescriptor MessageNotPartial = new(
        DiagnosticIDs.MessageNotPartial.ToIdString(),
        "Message class should be partial", "Message class {0} should be partial to enable source generated extensions.", "MintyCore.Generator",
        DiagnosticSeverity.Warning, true);
    
    public static Diagnostic MessageNotPartialDiagnostic(INamedTypeSymbol messageType)
    {
        return Diagnostic.Create(MessageNotPartial, messageType.Locations.FirstOrDefault(), messageType.ToString());
    }
}