using System;

namespace MintyCore.Generator;

[Flags]
public enum GenericConstraints
{
    None = 0,
    Constructor = 1 << 0,
    NotNull = 1 << 1,
    ReferenceType = 1 << 2,
    UnmanagedType = 1 << 3,
    ValueType = 1 << 4
}