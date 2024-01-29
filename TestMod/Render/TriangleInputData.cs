using System.Numerics;
using MintyCore.Graphics.Render;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using TestMod.Identifications;

namespace TestMod.Render;

[RegisterInputDataModule("triangle_input_module")]
public class TriangleInputData(IInputDataManager inputDataManager) : InputDataModule
{
    private DictionaryInputData<int, Triangle> _triangleData = null!;
    
    public override void Setup()
    {
        _triangleData = inputDataManager.GetDictionaryInputData<int, Triangle>(RenderInputDataIDs.TriangleInputData);
    }

    public override void Update(CommandBuffer cb)
    {
        using var holder = _triangleData.AcquireData(out var triangles);
    }

    public override Identification Identification => RenderInputModuleIDs.TriangleInputModule;
    

    public struct Triangle
    {
        public Vector3 Point1 { get; init; }
        public Vector3 Point2 { get; init; }
        public Vector3 Point3 { get; init; }

        public Vector4 Color { get; init; }
    }

    public override void Dispose()
    {
        
    }
    
    [RegisterKeyIndexedInputData("triangle_input_data")]
    public static DictionaryInputDataRegistryWrapper<int, Triangle> TriangleData => new();
}