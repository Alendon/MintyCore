
namespace MintyCore.Utils
{
    public static class TypeConversion
    {
        public static Ara3D.Vector3 ToAra3DVector(this System.Numerics.Vector3 vector3)
        {
            return new(vector3.X, vector3.Y, vector3.Z);
        }
        
        public static Ara3D.Vector2 ToAra2DVector(this System.Numerics.Vector2 vector2)
        {
            return new(vector2.X, vector2.Y);
        }
    }
}