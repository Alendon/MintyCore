using System;
using JetBrains.Annotations;
using MintyCore.Modding.Implementations;

namespace MintyCore.Modding.Attributes;

/// <summary>
/// Attribute which marks a register method to be used by the source generator
/// Also the <see cref="RegistryAttribute"/> needs to be applied to the class containing the method
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class RegisterMethodAttribute : Attribute
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="phase">Phase which the registry will execute in</param>
    /// <param name="options">Options for the registration</param>
    public RegisterMethodAttribute([UsedImplicitly] ObjectRegistryPhase phase,
        [UsedImplicitly] RegisterMethodOptions options = RegisterMethodOptions.None)
    {
    }
}

/// <summary>
/// Options for the <see cref="RegisterMethodAttribute"/>
/// </summary>
[Flags]
public enum RegisterMethodOptions
{
    /// <summary>
    /// No options
    /// </summary>
    None = 0,

    /// <summary>
    /// The registry depends on a resource file
    /// </summary>
    HasFile = 1 << 0,
}