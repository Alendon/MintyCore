namespace MintyCore.Graphics.Render;

public abstract class IntermediateData
{
    public abstract void Reset();
    public AccessMode AccessMode { get; set; }
}