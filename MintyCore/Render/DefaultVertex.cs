using Ara3D;
using System;
using Veldrid;

namespace MintyCore.Render
{
    public readonly struct DefaultVertex : IVertex
    {
        public readonly Vector3 Position;
        public readonly Vector3 Color;
        public readonly Vector3 Normal;
        public readonly Vector2 Uv;

        
        public DefaultVertex(Vector3 position, Vector3 color, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Color = color;
            Normal = normal;
            Uv = uv;
        }

        public VertexLayoutDescription GetVertexLayout()
        {
            return new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementFormat.Float3, VertexElementSemantic.Position),
                new VertexElementDescription("Color", VertexElementFormat.Float3, VertexElementSemantic.Color),
                new VertexElementDescription("Normal", VertexElementFormat.Float3, VertexElementSemantic.Normal),
                new VertexElementDescription("UV", VertexElementFormat.Float2,
                    VertexElementSemantic.TextureCoordinate));
        }

        public bool Equals(DefaultVertex other)
        {
            return Position.Equals(other.Position) && Color.Equals(other.Color) && Normal.Equals(other.Normal) && Uv.Equals(other.Uv);
        }

        public bool Equals(IVertex? other)
        {
            return other is DefaultVertex vertex && Equals(vertex);
        }

        public override bool Equals(object? obj)
        {
            return obj is DefaultVertex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Color, Normal, Uv);
        }

        public static bool operator ==(DefaultVertex left, DefaultVertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DefaultVertex left, DefaultVertex right)
        {
            return !(left == right);
        }
    }
}