using System;
using JetBrains.Annotations;

namespace MintyCore.Modding.Attributes;

/// <summary>
/// Helper attribute for source generation
/// </summary>
[UsedImplicitly]
[AttributeUsage(AttributeTargets.Class)]
public class ReferencedRegisterMethodAttribute<T> : Attribute where T : RegisterMethodInfo;