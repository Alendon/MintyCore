using System;
using JetBrains.Annotations;

namespace MintyCore.Modding.Attributes;

[UsedImplicitly]
[AttributeUsage(AttributeTargets.Class)]
public class ReferencedRegisterMethodAttribute<T> : Attribute where T : RegisterMethodInfo;