using System;
using System.Numerics;
using MintyVeldrid;

namespace MintyCore.Render
{
    /// <summary>
    ///     The default Vertex implementation used in MintyCore
    /// </summary>
    public readonly struct DefaultVertex : IVertex, IEquatable<DefaultVertex>
    {
        /// <summary>
        ///     Position Value of the Vertex
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        ///     Color Value of the Vertex
        /// </summary>
        public readonly Vector3 Color;

        /// <summary>
        ///     Normal Value of the Vertex
        /// </summary>
        public readonly Vector3 Normal;

        /// <summary>
        ///     Uv Value of the Vertex
        /// </summary>
        public readonly Vector2 Uv;

        /// <summary>
        ///     Create a new Vertex
        /// </summary>
        public DefaultVertex(Vector3 position, Vector3 color, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Color = color;
            Normal = normal;
            Uv = uv;
        }

        /// <inheritdoc />
        public VertexLayoutDescription GetVertexLayout()
        {
            return new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementFormat.Float3, VertexElementSemantic.Position),
                new VertexElementDescription("Color", VertexElementFormat.Float3, VertexElementSemantic.Color),
                new VertexElementDescription("Normal", VertexElementFormat.Float3, VertexElementSemantic.Normal),
                new VertexElementDescription("UV", VertexElementFormat.Float2,
                    VertexElementSemantic.TextureCoordinate));
        }

        /// <inheritdoc />
        public bool Equals(DefaultVertex other)
        {
            return Position.Equals(other.Position) && Color.Equals(other.Color) && Normal.Equals(other.Normal) &&
                   Uv.Equals(other.Uv);
        }

        /// <inheritdoc />
        public bool Equals(IVertex? other)
        {
            return other is DefaultVertex vertex && Equals(vertex);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is DefaultVertex other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Color, Normal, Uv);
        }

        /// <summary>
        ///     Operator to check if two <see cref="DefaultVertex" /> are equal
        /// </summary>
        public static bool operator ==(DefaultVertex left, DefaultVertex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Operator to check if two <see cref="DefaultVertex" /> are not equal
        /// </summary>
        public static bool operator !=(DefaultVertex left, DefaultVertex right)
        {
            return !(left == right);
        }
    }
}