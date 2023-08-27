using System;

namespace MintyCore.Generator.Registry;

[Flags]
public enum RegisterMethodOptions
{
    None = 0,
    HasFile = 1 << 0,
    UseExistingId = 1 << 1
}