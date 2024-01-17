using System;
using System.Collections.Generic;
using MintyCore.Graphics.RenderGraph.RenderResources;
using MintyCore.Utils;
using QuikGraph;
using QuikGraph.Algorithms;
using Serilog;

namespace MintyCore.Graphics.RenderGraph.Implementations;

[Singleton<IRenderManager>(SingletonContextFlags.NoHeadless)]
public class RenderManager : IRenderManager
{
    private Dictionary<Identification, RenderModuleDescription> _renderModuleDescriptions = new();
    private Dictionary<Identification, TextureResourceDescription> _textureResourceDescriptions = new();
    private Identification _swapchainResourceId;
    
    private HashSet<Identification> _activeRenderModules = new();
    
    public void AddRenderModule(Identification id, RenderModuleDescription renderModuleDescription)
    {
        _renderModuleDescriptions.Add(id, renderModuleDescription);
        
        if(renderModuleDescription.ActiveByDefault)
            _activeRenderModules.Add(id);
    }

    public void AddTextureResource(Identification id, TextureResourceDescription textureResourceDescription)
    {
        _textureResourceDescriptions.Add(id, textureResourceDescription);
    }
    
    public void SetSwapchainResource(Identification id)
    {
        _swapchainResourceId = id;
    }

    public void SetRenderModuleActive(Identification id, bool active)
    {
        throw new System.NotImplementedException();
    }

    public bool IsRenderModuleActive(Identification id)
    {
        throw new System.NotImplementedException();
    }

    public void ConstructRenderGraph()
    {
        var graph = new AdjacencyGraph<Identification, Edge<Identification>>();
        graph.AddVertexRange(_activeRenderModules);

        ApplyExplicitOrdering(graph);

        
        
        graph.TrimEdgeExcess();

        if (!graph.IsDirectedAcyclicGraph())
        {
            
        }
    }

    private void ApplyExplicitOrdering(AdjacencyGraph<Identification, Edge<Identification>> graph)
    {
        foreach (var renderModuleId in graph.Vertices)
        {
            var renderModuleDescription = _renderModuleDescriptions[renderModuleId];
            
            if(renderModuleDescription.ordering is null) continue;

            var ordering = renderModuleDescription.ordering.Value;
            foreach (var (otherModuleId, (order, moduleMustExist)) in ordering)
            {
                if (!graph.ContainsVertex(otherModuleId))
                {
                    if(moduleMustExist)
                        throw new InvalidOperationException($"Render module {renderModuleId} requires render module {otherModuleId} to exist, but it does not.");
                    continue;
                }

                graph.AddEdge(order == RenderModuleOrdering.Before
                    ? new Edge<Identification>(otherModuleId, renderModuleId)
                    : new Edge<Identification>(renderModuleId, otherModuleId));
            }
            
        }
    }

    public void BeginRendering()
    {
        throw new System.NotImplementedException();
    }

    public void EndRendering()
    {
        throw new System.NotImplementedException();
    }

    public int FPS { get; set; }
    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}