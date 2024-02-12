using System;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Utils;

/// <summary>
///     The default Vertex implementation used in MintyCore
/// </summary>
[PublicAPI]
[StructLayout(LayoutKind.Explicit)]
public readonly unsafe struct Vertex : IEquatable<Vertex>
{
    private const int PositionOffset = 0;
    private const int ColorOffset = PositionOffset + sizeof(float) * 3;
    private const int NormalOffset = ColorOffset + sizeof(float) * 3;
    private const int UvOffset = NormalOffset + sizeof(float) * 3;

    /// <summary>
    ///     Position Value of the Vertex
    /// </summary>
    [FieldOffset(PositionOffset)] public readonly Vector3 Position;

    /// <summary>
    ///     Color Value of the Vertex
    /// </summary>
    [FieldOffset(ColorOffset)] public readonly Vector3 Color;

    /// <summary>
    ///     Normal Value of the Vertex
    /// </summary>
    [FieldOffset(NormalOffset)] public readonly Vector3 Normal;

    /// <summary>
    ///     Uv Value of the Vertex
    /// </summary>
    [FieldOffset(UvOffset)] public readonly Vector2 Uv;

    /// <summary>
    ///     Create a new Vertex
    /// </summary>
    public Vertex(Vector3 position, Vector3 color, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Color = color;
        Normal = normal;
        Uv = uv;
    }

    /// <summary>
    ///     Get the input bindings for the vertex
    /// </summary>
    /// <returns></returns>
    public static VertexInputBindingDescription GetVertexBinding()
    {
        return new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint) sizeof(Vertex),
            InputRate = VertexInputRate.Vertex
        };
    }

    /// <summary>
    ///     Get the input attribute for the vertex
    /// </summary>
    /// <returns></returns>
    public static VertexInputAttributeDescription[] GetVertexAttributes()
    {
        return new[]
        {
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Format = Format.R32G32B32Sfloat,
                Location = 0,
                Offset = PositionOffset
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Format = Format.R32G32B32Sfloat,
                Location = 1,
                Offset = ColorOffset
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Format = Format.R32G32B32Sfloat,
                Location = 2,
                Offset = NormalOffset
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Format = Format.R32G32Sfloat,
                Location = 3,
                Offset = UvOffset
            }
        };
    }

    /// <inheritdoc />
    public bool Equals(Vertex other)
    {
        return Position.Equals(other.Position) && Color.Equals(other.Color) && Normal.Equals(other.Normal) &&
               Uv.Equals(other.Uv);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Vertex other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Color, Normal, Uv);
    }

    /// <summary>
    ///     Operator to check if two <see cref="Vertex" /> are equal
    /// </summary>
    public static bool operator ==(Vertex left, Vertex right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Operator to check if two <see cref="Vertex" /> are not equal
    /// </summary>
    public static bool operator !=(Vertex left, Vertex right)
    {
        return !(left == right);
    }
}