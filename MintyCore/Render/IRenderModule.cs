using MintyCore.Utils;

namespace MintyCore.Render;

public interface IRenderModuleOutput<out TOutput> : IRenderModule
{
    /// <summary>
    /// Get or create the output data created by this module in the next frame
    /// </summary>
    /// <returns> The output data </returns>
    TOutput GetOrCreateOutput();
    
    static abstract Identification GetOutputId();
}

public interface IRenderModule
{
    void Process();
}