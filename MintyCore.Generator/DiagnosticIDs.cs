namespace MintyCore.Generator;

public static class DiagnosticIdExtensions
{
    public static string ToIdString(this DiagnosticIDs id)
    {
        return $"MC{(uint)id:D4}";
    }
}


public enum DiagnosticIDs : uint
{
    //The first digit is the category
    //1: Registry stuff
    //2: Mod stuff
    //3: Misc
    //The second digit is the severity
    //1: Warning
    //2: Error
    //The last two digits are the error code
    
    Invalid = 0,
    NoRegisterMethods = 1101,
    InvalidRegisterMethod = 1202,
    InvalidRegisterAttribute = 1203,
    InvalidGenericTypeForRegistry = 1204,
    InvalidPropertyTypeForRegistry = 1205,

    OnlyOneModPerAssembly = 2201,
    NeedOneModInAssembly = 2202,
    PublicModClass = 2101,
    SealedModClass = 2102,
    
    MessageNested = 3101,
    MessageNotPartial = 3102,
    WorldConstructorNeedsIsServerWorld = 3201,
}