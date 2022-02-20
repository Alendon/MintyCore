using System;

namespace MintyCore.ECS;

/// <summary>
///     Attribute to mark a system automatically running parallel.
///     The system must be partial, needs exactly one component query and one custom "Execute" Method which takes the
///     ComponentQuery.CurrentEntity as a parameter
/// </summary>
public class ParallelSystemAttribute : Attribute
{
}