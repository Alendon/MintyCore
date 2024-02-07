namespace MintyCore.Graphics.Render.Managers;

public interface IRenderManager
{
    void StartRendering();
    void StopRendering();
    
    int MaxFrameRate { get; set; }
    int FrameRate { get; }
}