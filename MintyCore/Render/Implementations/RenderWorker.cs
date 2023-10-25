using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Core;
using Autofac.Core.Registration;
using MintyCore.Utils;
using QuikGraph;
using QuikGraph.Algorithms;

namespace MintyCore.Render.Implementations;

internal class RenderWorker : IRenderWorker
{
    private volatile bool _stopRequested;
    private Thread _workerThread;
    
    private Stopwatch _stopwatch = new();
    private double _frameTime;

    /// <inheritdoc />
    public int MaxFrameRate
    {
        set => _frameTime = 1.0 / value;
    }
    
    private int _frameRate;
    public int FrameRate => _frameRate;

    private int _renderedFrames;
    private double _sinceLastFrame;
    private double _sinceLastFpsUpdate;

    private readonly IRenderInputManager _renderInputManager;
    private readonly IRenderManager _renderManager;
    private readonly IRenderOutputManager _renderOutputManager;
    private readonly IVulkanEngine _vulkanEngine;

    private readonly Dictionary<Identification, Dictionary<Identification, Func<object>>>
        _renderModuleOutputProviders = new();

    private readonly Dictionary<Identification, Dictionary<Identification, Action<IRenderInput>>>
        _renderModuleInputDependencies = new();

    private readonly Dictionary<Identification, Dictionary<Identification, Action<object>>>
        _renderModuleOutputDependencies = new();

    private readonly Dictionary<Identification, IRenderModule> _renderModules = new();
    private readonly Dictionary<Identification, IRenderInput> _renderInputs = new();

    //Key: OutputId, Value: ModuleId
    private readonly Dictionary<Identification, Identification> _reversedOutputProvider = new();

    //Sorry for this "mess" xD
    //This is basically the ordering with all what needs to be done to render everything
    //TODO C#13 introduce a custom using statement to make this more readable
    private (IRenderModule renderModule, (IRenderInput source, Action<IRenderInput> destination)[] inputDependencies,
        (Func<object> source, Action<object> destination)[] outputDependencies)[]
        _renderModuleProcessing
            = Array.Empty<(IRenderModule, (IRenderInput, Action<IRenderInput>)[],
                (Func<object>, Action<object> )[])>();

    private (IRenderInput, int[] dependencyIndices)[] _renderInputProcessing = Array.Empty<(IRenderInput, int[])>();

    public RenderWorker(IRenderInputManager inputManager, IRenderManager renderManager, IVulkanEngine vulkanEngine,
        IRenderOutputManager renderOutputManager)
    {
        _workerThread = new Thread(Work);
        _renderInputManager = inputManager;
        _renderManager = renderManager;
        _vulkanEngine = vulkanEngine;
        _renderOutputManager = renderOutputManager;
    }

    private void Work()
    {
        Initialize();
        WorkerLoop();
    }

    private void WorkerLoop()
    {
        _stopwatch.Start();
        while (!_stopRequested)
        {
            bool render = UpdateTime();
            if(!render) continue;
            
            var inputTask = ProcessInputs();

            if (!_vulkanEngine.PrepareDraw())
            {
                //TODO make sure that this never happens
                throw new MintyCoreException("Oh no, vulkan failed to prepare drawing");
            }

            inputTask.Wait();

            ProcessRenderModules();

            _vulkanEngine.EndDraw();
        }
        
    }

    private bool UpdateTime()
    {
        var elapsed = _stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();
        _sinceLastFrame += elapsed;
        _sinceLastFpsUpdate += elapsed;
        
        if (_sinceLastFpsUpdate >= 1)
        {
            _frameRate = _renderedFrames;
            _renderedFrames = 0;
            
            _sinceLastFpsUpdate = 0;
        }
        
        if (_sinceLastFrame < _frameTime) return false;
        
        _sinceLastFrame = 0;
        _renderedFrames++;
        return true;
    }

    private void ProcessRenderModules()
    {
        var cb = _vulkanEngine.GetRenderCommandBuffer();

        foreach (var (renderModule, inputDependencies, outputDependencies) in _renderModuleProcessing)
        {
            //Process all input dependencies
            foreach (var (input, callback) in inputDependencies)
            {
                callback(input);
            }

            //Process all output dependencies
            foreach (var (source, destination) in outputDependencies)
            {
                destination(source());
            }

            //Process the render module
            renderModule.Process(cb);
        }
    }

    private Task ProcessInputs()
    {
        var tasks = new Task[_renderInputProcessing.Length];

        for (var i = 0; i < _renderInputProcessing.Length; i++)
        {
            var (input, dependencies) = _renderInputProcessing[i];
            if (dependencies.Length == 0)
            {
                tasks[i] = input.Process();
                continue;
            }

            tasks[i] = Task.WhenAll(dependencies.Select(x => tasks[x]))
                .ContinueWith(_ => input.Process());
        }

        return Task.WhenAll(tasks);
    }

    private void Initialize()
    {
        //Query all render modules
        foreach (var moduleId in _renderManager.ActiveRenderModules)
        {
            var module = _renderManager.GetRenderModule(moduleId);
            module.Initialize(this);
            _renderModules.Add(moduleId, module);
        }

        //Query all input dependencies
        foreach (var inputId in _renderModuleInputDependencies.Values.SelectMany(x => x.Keys))
        {
            if (_renderInputs.ContainsKey(inputId)) continue;

            _renderInputs.Add(inputId, _renderInputManager.GetRenderInput(inputId));
        }

        //Query all hidden input dependencies
        foreach (var input in _renderInputs.Values)
        {
            foreach (var inputDependencies in input.ExecuteAfter)
            {
                if (_renderInputs.ContainsKey(inputDependencies)) continue;
                _renderInputs.Add(inputDependencies, _renderInputManager.GetRenderInput(inputDependencies));
            }
        }

        var inputGraph = BuildRenderInputGraph();
        BuildInputProcessing(inputGraph);

        FlattenOutputProviders();

        var renderModuleGraph = BuildRenderModuleGraph();

        BuildModuleProcessing(renderModuleGraph.TopologicalSort().ToArray());
    }

    private void BuildInputProcessing(BidirectionalGraph<Identification, Edge<Identification>> graph)
    {
        _renderInputProcessing = new (IRenderInput, int[])[_renderInputs.Count];

        var order = graph.TopologicalSort().ToArray();
        var reversedOrder = order.Select((value, index) => (value, index))
            .ToDictionary(a => a.value, b => b.index);

        for (var index = 0; index < order.Length; index++)
        {
            var inputId = order[index];
            _renderInputProcessing[index].Item1 = _renderInputs[inputId];

            var inEdges = graph.InEdges(inputId).ToArray();
            _renderInputProcessing[index].Item2 = new int[inEdges.Length];

            for (var i = 0; i < inEdges.Length; i++)
            {
                _renderInputProcessing[index].Item2[i] = reversedOrder[inEdges[i].Source];
            }
        }
    }

    private BidirectionalGraph<Identification, Edge<Identification>> BuildRenderInputGraph()
    {
        //put all render inputs inside the graph
        var graph = new BidirectionalGraph<Identification, Edge<Identification>>();
        foreach (var inputId in _renderInputs.Keys)
        {
            graph.AddVertex(inputId);
        }

        //connect the dependent render inputs
        foreach (var (inputId, renderInput) in _renderInputs)
        {
            foreach (var before in renderInput.ExecuteAfter)
            {
                graph.AddEdge(new Edge<Identification>(before, inputId));
            }

            foreach (var after in renderInput.ExecuteBefore)
            {
                if (!graph.ContainsVertex(after)) continue;

                graph.AddEdge(new Edge<Identification>(inputId, after));
            }
        }

        //check for cycles
        if (!graph.IsDirectedAcyclicGraph())
        {
            throw new MintyCoreException("Circular dependency detected in render inputs.");
        }

        return graph;
    }

    private void BuildModuleProcessing(IReadOnlyList<Identification> moduleOrder)
    {
        _renderModuleProcessing = new (IRenderModule,
            (IRenderInput, Action<IRenderInput>)[] inputDependencies,
            (Func<object> source, Action<object> destination)[] outputDependencies)
            [_renderModules.Count];

        for (var i = 0; i < moduleOrder.Count; i++)
        {
            _renderModuleProcessing[i].renderModule = _renderModules[moduleOrder[i]];

            //Get all input dependencies
            if (_renderModuleInputDependencies.TryGetValue(moduleOrder[i], out var inputDependencies))
            {
                _renderModuleProcessing[i].inputDependencies =
                    new (IRenderInput, Action<IRenderInput>)[inputDependencies.Count];

                var j = 0;
                foreach (var (inputId, callback) in inputDependencies)
                {
                    _renderModuleProcessing[i].inputDependencies[j] = (_renderInputs[inputId], callback);
                    j++;
                }
            }
            else
            {
                _renderModuleProcessing[i].inputDependencies = Array.Empty<(IRenderInput, Action<IRenderInput>)>();
            }

            //Get all output dependencies
            if (_renderModuleOutputDependencies.TryGetValue(moduleOrder[i], out var outputDependencies))
            {
                _renderModuleProcessing[i].outputDependencies =
                    new (Func<object> source, Action<object> destination)[outputDependencies
                        .Count];

                var j = 0;
                foreach (var (outputId, callback) in outputDependencies)
                {
                    _renderModuleProcessing[i].outputDependencies[j] = (
                        _renderModuleOutputProviders[_reversedOutputProvider[outputId]][outputId], callback);
                    j++;
                }
            }
            else
            {
                _renderModuleProcessing[i].outputDependencies =
                    Array.Empty<(Func<object> source, Action<object> destination)>();
            }
        }
    }

    private void FlattenOutputProviders()
    {
        var duplicateOutputProviders = new HashSet<(Identification, Identification)>();

        //The linq expression creates a pair (moduleId, outputId) for each output provider
        foreach (var (moduleId, outputId) in _renderModuleOutputProviders.SelectMany(x =>
                     x.Value.Keys.Select(y => (x.Key, y))))
        {
            if (!_reversedOutputProvider.TryAdd(outputId, moduleId))
            {
                duplicateOutputProviders.Add((moduleId, outputId));
            }
        }

        if (duplicateOutputProviders.Count <= 0) return;

        var sb = new StringBuilder();

        sb.AppendLine("Duplicate output providers detected:");
        foreach (var (moduleId, outputId) in duplicateOutputProviders)
        {
            sb.AppendLine($"Module: {moduleId}, Output: {outputId}");
        }

        throw new MintyCoreException(sb.ToString());
    }

    private AdjacencyGraph<Identification, Edge<Identification>> BuildRenderModuleGraph()
    {
        //put all render modules inside the graph
        var graph = new AdjacencyGraph<Identification, Edge<Identification>>();
        foreach (var moduleId in _renderModules.Keys)
        {
            graph.AddVertex(moduleId);
        }

        //connect the dependent render modules
        foreach (var (moduleId, outputDependency) in _renderModuleOutputDependencies)
        {
            foreach (var (outputId, _) in outputDependency)
            {
                var outputProvider = _reversedOutputProvider[outputId];
                graph.AddEdge(new Edge<Identification>(outputProvider, moduleId));
            }
        }

        //check for cycles
        if (!graph.IsDirectedAcyclicGraph())
        {
            throw new MintyCoreException("Circular dependency detected in render modules.");
        }

        return graph;
    }

    public void Start()
    {
        _workerThread.Start();
    }

    public void Stop()
    {
        _stopRequested = true;

        _workerThread.Join();
        _workerThread = new Thread(Work);
    }

    /// <inheritdoc />
    public bool IsRunning()
    {
        return _workerThread.IsAlive;
    }

    /// <inheritdoc />
    public void SetInputDependencyNew<TRenderInput>(Identification renderModuleId, Identification inputId,
        Action<TRenderInput> callback) where TRenderInput : class, IRenderInput
    {
        try
        {
            var input = _renderInputManager.GetRenderInput(inputId);
            if (input is not TRenderInput)
                //throw an exception with a message thats states a missmatch between TRenderInput and type of input
                throw new MintyCoreException(
                    $"Type of provided input callback ({typeof(TRenderInput)}) does not match the type of the input ({input.GetType()})");
        }
        catch (Exception e) when (e is DependencyResolutionException or ComponentNotRegisteredException)
        {
            throw new MintyCoreException("Failed to resolve render input", e);
        }

        if (!_renderModuleInputDependencies.ContainsKey(renderModuleId))
            _renderModuleInputDependencies.Add(renderModuleId, new Dictionary<Identification, Action<IRenderInput>>());

        _renderModuleInputDependencies[renderModuleId].Add(inputId, input =>
            {
                if (input is not TRenderInput typedInput)
                    throw new MintyCoreException($"Input is not of type TRenderInput ({typeof(TRenderInput)})");

                callback(typedInput);
            }
        );
    }

    /// <inheritdoc />
    public void SetOutputDependencyNew<TModuleOutput>(Identification renderModuleId, Identification outputId,
        Action<TModuleOutput> callback) where TModuleOutput : class
    {
        var outputType = _renderOutputManager.GetRenderOutputType(outputId);
        if (!typeof(TModuleOutput).IsAssignableFrom(outputType))
            //throw an exception with a message thats states a missmatch between TRenderInput and type of input
            throw new MintyCoreException(
                $"Type of provided output callback ({typeof(TModuleOutput)}) does not match the type of the input ({outputId})");

        if (!_renderModuleOutputDependencies.ContainsKey(renderModuleId))
            _renderModuleOutputDependencies.Add(renderModuleId,
                new Dictionary<Identification, Action<object>>());

        _renderModuleOutputDependencies[renderModuleId].Add(outputId, output =>
        {
            if (output is not TModuleOutput typedOutput)
                throw new MintyCoreException($"Output is not of type TModuleOutput ({typeof(TModuleOutput)})");

            callback(typedOutput);
        });
    }

    /// <inheritdoc />
    public void SetOutputProviderNew<TModuleOutput>(Identification renderModuleId, Identification outputId,
        Func<TModuleOutput> outputGetter) where TModuleOutput : class
    {
        var outputType = _renderOutputManager.GetRenderOutputType(outputId);
        if (!typeof(TModuleOutput).IsAssignableFrom(outputType))
            //throw an exception with a message thats states a missmatch between TRenderInput and type of input
            throw new MintyCoreException(
                $"Type of provided output callback ({typeof(TModuleOutput)}) does not match the type of the input ({outputId})");

        if (!_renderModuleOutputProviders.ContainsKey(renderModuleId))
            _renderModuleOutputProviders.Add(renderModuleId,
                new Dictionary<Identification, Func<object>>());

        _renderModuleOutputProviders[renderModuleId].Add(outputId,
            () => outputGetter() ??
                  throw new MintyCoreException($"Output is null"));
    }
}