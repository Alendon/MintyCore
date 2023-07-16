namespace MintyCoreGenerator;

public static class DiagnosticIdExtensions
{
    public static string ToIdString(this DiagnosticIDs id)
    {
        return $"MC{(uint)id:D4}";
    }
}

public enum DiagnosticIDs : uint
{
    Invalid = 0,
    NoRegisterMethods = 1201,
    InvalidRegisterMethod = 1202,
    InvalidRegisterAttribute = 1203,
    InvalidGenericTypeForRegistry = 1204,
    InvalidPropertyTypeForRegistry = 1205,

    OnlyOneModPerAssembly = 2201,
    NeedOneModInAssembly = 2202,
    PublicModClass = 2101,
    SealedModClass = 2102,
    PartialModClass = 2103,
}