using Microsoft.CodeAnalysis;

namespace MintyCoreGenerator.Registry;

public static class DiagnosticsHelper
{
    private static readonly DiagnosticDescriptor _noRegisterMethodsDescriptor = new(DiagnosticIDs.NoRegisterMethods.ToIdString(),
        "No Register Methods found", "No Register Methods found for Registry {0}.", "MintyCoreGenerator",
        DiagnosticSeverity.Warning, true);

    public static Diagnostic NoRegisterMethods(Location? location, string className)
    {
        return Diagnostic.Create(_noRegisterMethodsDescriptor, location, className);
    }

    private static readonly DiagnosticDescriptor _invalidRegisterMethodDescriptor = new(
        DiagnosticIDs.InvalidReigsterMethod.ToIdString(),
        "Invalid Register Method", "Invalid Register Method.", "MintyCoreGenerator",
        DiagnosticSeverity.Error, true);
    
    public static Diagnostic InvalidRegisterMethod(Location? location, string methodName)
    {
        return Diagnostic.Create(_invalidRegisterMethodDescriptor, location, methodName);
    }
}