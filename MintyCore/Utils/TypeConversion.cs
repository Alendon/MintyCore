
namespace MintyCore.Utils
{
    /// <summary>
    /// Collection of TypeConversionMethods
    /// </summary>
    public static class TypeConversion
    {
        /// <summary>
        /// ExtensionMethod to Convert <see cref="System.Numerics.Vector3"/> to <see cref="Ara3D.Vector3"/>
        /// </summary>
        public static Ara3D.Vector3 ToAra3DVector(this System.Numerics.Vector3 vector3)
        {
            return new(vector3.X, vector3.Y, vector3.Z);
        }

        /// <summary>
        /// ExtensionMethod to Convert <see cref="System.Numerics.Vector2"/> to <see cref="Ara3D.Vector2"/>
        /// </summary>
        public static Ara3D.Vector2 ToAra2DVector(this System.Numerics.Vector2 vector2)
        {
            return new(vector2.X, vector2.Y);
        }
    }
}