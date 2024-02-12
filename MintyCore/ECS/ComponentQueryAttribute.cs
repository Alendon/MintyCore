using System;

namespace MintyCore.ECS;

/// <summary>
///     Attribute to mark a field to be a auto generated component query.
///     Also mark the class as partial to function
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ComponentQueryAttribute : Attribute;