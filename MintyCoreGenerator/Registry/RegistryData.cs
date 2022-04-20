using System;

namespace MintyCoreGenerator.Registry;

public class RegistryData
{
    
}

class RegisterMethod
{
    public readonly RegisterMethodType RegisterMethodType;
    public readonly string MethodName;
    public readonly string FullMethodName;
    public readonly int RegistryPhase;

    public readonly bool HasFile;

    public readonly string[]? GenericConstraintTypes;
    public readonly GenericConstraints? GenericConstraints;

    public readonly string? PropertyType;

    public RegisterMethod(RegisterMethodType registerMethodType, string methodName, string fullMethodName, int registryPhase)
    {
        RegisterMethodType = registerMethodType;
        MethodName = methodName;
        FullMethodName = fullMethodName;
        RegistryPhase = registryPhase;
    }
}

[Flags]
enum GenericConstraints
{
    None = 0,
    Unmanaged = 1 << 0,
    Class = 1 << 1,
    Struct = 1 << 2,
    New = 1 << 3,
    
}

enum RegisterMethodType
{
    Invalid = 0,
    Property = 1,
    Generic = 2,
    File = 3
}