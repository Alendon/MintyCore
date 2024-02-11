#pragma warning disable

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptsLocationAttribute(string filePath, int line, int column) : Attribute;