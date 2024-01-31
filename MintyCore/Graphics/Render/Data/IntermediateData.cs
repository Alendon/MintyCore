namespace MintyCore.Graphics.Render.Data;

public abstract class IntermediateData
{
    public abstract void Reset();
    public AccessMode AccessMode { get; set; }

    /// <summary>
    /// Copy from the previous set IntermediateData
    /// This method should only be overridden if the IntermediateData contains references to other objects which should be shared
    /// </summary>
    /// <param name="previousData"></param>
    public virtual void CopyFrom(IntermediateData? previousData)
    {
        
    }
}