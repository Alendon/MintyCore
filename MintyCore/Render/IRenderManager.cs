using System;
using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Render;

public interface IRenderManager : IDisposable
{
    void AddRenderModule<TRenderModule>(Identification renderModuleId) where TRenderModule : IRenderModule;
    void RemoveRenderModule(Identification renderModuleId);
    IRenderModule GetRenderModule(Identification renderModuleId);
    void ConstructRenderModules();
    
    /// <summary>
    /// Set the render module active or inactive. If the render module is inactive, it will not be processed.
    /// <see cref="Recreate"/> must be called after changing the active state of a render module to apply the changes.
    /// </summary>
    /// <param name="renderModuleId"> The render module to change the active state of.</param>
    /// <param name="active"> The new active state of the render module.</param>
    void SetRenderModuleActive(Identification renderModuleId, bool active);
    bool IsRenderModuleActive(Identification renderModuleId);
    IEnumerable<Identification> ActiveRenderModules { get; }
    
    void StartRendering();
    void StopRendering();
    bool IsRendering { get; }
 
    void Recreate();
    
    int FrameRate { get; }
    int MaxFrameRate { get; set; }
}