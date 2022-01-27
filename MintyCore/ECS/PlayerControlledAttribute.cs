using System;

namespace MintyCore.ECS;

/// <summary>
/// Attribute to mark an <see cref="IComponent"/> as player controlled
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public class PlayerControlledAttribute : Attribute
{
}