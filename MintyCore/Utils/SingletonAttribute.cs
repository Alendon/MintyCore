using System;
using JetBrains.Annotations;

namespace MintyCore.Utils;

/// <summary>
/// Base class for the Singleton attribute
/// </summary>
public abstract class BaseSingletonAttribute : Attribute
{
    /// <summary>
    /// The type that the attribute is implemented on
    /// </summary>
    public abstract Type ImplementedType { get; }
    
    /// <summary>
    ///  The context flags for the attribute
    /// </summary>
    public abstract SingletonContextFlags ContextFlags { get; }
}

/// <summary>
/// Attribute to mark a class as a singleton
/// </summary>
/// <typeparam name="TImplemented"> The type that the annotated class is implemented on </typeparam>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonAttribute<TImplemented> : BaseSingletonAttribute where TImplemented : class
{
    /// <inheritdoc />
    public override Type ImplementedType => typeof(TImplemented);

    /// <inheritdoc />
    public override SingletonContextFlags ContextFlags { get; }
    
    /// <summary>
    /// Instantiate a new <see cref="SingletonAttribute{TImplemented}" /> instance
    /// </summary>
    /// <param name="contextFlags"> The context flags for the attribute </param>
    public SingletonAttribute(SingletonContextFlags contextFlags = SingletonContextFlags.None)
    {
        ContextFlags = contextFlags;
    }
}

//flags for conditional registration
/// <summary>
/// Flags for conditional registration
/// </summary>
[Flags]
public enum SingletonContextFlags
{
    /// <summary>
    ///   No context flags
    /// </summary>
    None = 0,
    /// <summary>
    ///  Do not register the type as a singleton in headless mode
    /// </summary>
    NoHeadless = 1,
}