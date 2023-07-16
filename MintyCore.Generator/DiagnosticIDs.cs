namespace MintyCoreGenerator;

public static class DiagnosticIdExtensions
{
    public static string ToIdString(this DiagnosticIDs id)
    {
        return $"MC{(uint) id:D4}";
    }
}

public enum DiagnosticIDs : uint
{
    Invalid = 0,
    NoRegisterMethods,
    InvalidRegisterMethod,
    InvalidRegisterAttribute,
    InvalidGenericTypeForRegistry,
    InvalidPropertyTypeForRegistry,
    OnlyOneModPerAssembly,
    NeedOneModInAssembly
}