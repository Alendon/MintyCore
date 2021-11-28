using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace MintyCore.Render
{
    /// <summary>
    ///     The default Vertex implementation used in MintyCore
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly unsafe struct DefaultVertex : IVertex, IEquatable<DefaultVertex>
    {
        private const int POSITION_OFFSET = 0;
        private const int COLOR_OFFSET = POSITION_OFFSET + (sizeof(float) * 3);
        private const int NORMAL_OFFSET = COLOR_OFFSET + (sizeof(float) * 3);
        private const int UV_OFFSET = NORMAL_OFFSET + (sizeof(float) * 3);

        /// <summary>
        ///     Position Value of the Vertex
        /// </summary>
        [FieldOffset(POSITION_OFFSET)]
        public readonly Vector3 Position;

        /// <summary>
        ///     Color Value of the Vertex
        /// </summary>
        [FieldOffset(COLOR_OFFSET)]
        public readonly Vector3 Color;

        /// <summary>
        ///     Normal Value of the Vertex
        /// </summary>
        [FieldOffset(NORMAL_OFFSET)]
        public readonly Vector3 Normal;

        /// <summary>
        ///     Uv Value of the Vertex
        /// </summary>
        [FieldOffset(UV_OFFSET)]
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
        public VertexInputBindingDescription[] GetVertexBindings()
        {
            return new[]
            {
                new VertexInputBindingDescription
                {
                    Binding = 0,
                    Stride = (uint)sizeof(DefaultVertex),
                    InputRate = VertexInputRate.Vertex
                }
            };
        }

        public VertexInputAttributeDescription[] GetVertexAttributes()
        {
            return new[]
            {
                new VertexInputAttributeDescription
                {
                    Binding = 0,
                    Format = Format.R32G32B32Sfloat,
                    Location = 0,
                    Offset = POSITION_OFFSET
                },
                new VertexInputAttributeDescription
                {
                    Binding = 0,
                    Format = Format.R32G32B32Sfloat,
                    Location = 1,
                    Offset = COLOR_OFFSET
                },
                new VertexInputAttributeDescription
                {
                    Binding = 0,
                    Format = Format.R32G32B32Sfloat,
                    Location = 2,
                    Offset = NORMAL_OFFSET
                },
                new VertexInputAttributeDescription
                {
                    Binding = 0,
                    Format = Format.R32G32Sfloat,
                    Location = 3,
                    Offset = UV_OFFSET
                },
            };
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