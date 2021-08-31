using System.Numerics;

namespace MintyCore.Render
{
    internal struct GpuCameraData
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
        public Matrix4x4 ViewProjection;
    }
}