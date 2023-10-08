using System;
using JetBrains.Annotations;

namespace MintyCore.Utils;

public abstract class BaseSingletonAttribute : Attribute
{
    public abstract Type ImplementedType { get; }
    public abstract SingletonContextFlags ContextFlags { get; }
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonAttribute<TImplemented> : BaseSingletonAttribute where TImplemented : class
{
    public override Type ImplementedType => typeof(TImplemented);
    public override SingletonContextFlags ContextFlags { get; }
    
    public SingletonAttribute(SingletonContextFlags contextFlags = SingletonContextFlags.None)
    {
        ContextFlags = contextFlags;
    }
}

//flags for conditional registration
[Flags]
public enum SingletonContextFlags
{
    None = 0,
    NoHeadless = 1,
}