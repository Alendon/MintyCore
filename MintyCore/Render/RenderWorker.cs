using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MintyCore.Utils;
using QuikGraph;
using QuikGraph.Algorithms;

namespace MintyCore.Render;

internal class RenderWorker : IRenderWorker
{
    private volatile bool _stopRequested;
    private Thread _workerThread;

    private IRenderInputManager _renderInputManager;
    private IRenderManager _renderManager;
    private IVulkanEngine _vulkanEngine;

    private readonly Dictionary<Identification, Dictionary<Identification, Func<IRenderOutputWrapper>>>
        _renderModuleOutputProviders = new();

    private readonly Dictionary<Identification, Dictionary<Identification, Action<IRenderInput>>>
        _renderModuleInputDependencies = new();

    private readonly Dictionary<Identification, Dictionary<Identification, Action<IRenderOutputWrapper>>>
        _renderModuleOutputDependencies = new();

    private readonly Dictionary<Identification, IRenderModule> _renderModules = new();
    private readonly Dictionary<Identification, IRenderInput> _renderInputs = new();

    //Key: OutputId, Value: ModuleId
    private readonly Dictionary<Identification, Identification> _reversedOutputProvider = new();

    //Sorry for this "mess" xD
    //This is basically the ordering with all what needs to be done to render everything
    //TODO C#13 introduce a custom using statement to make this more readable
    private (IRenderModule renderModule, (IRenderInput source, Action<IRenderInput> destination)[] inputDependencies,
        (Func<IRenderOutputWrapper> source, Action<IRenderOutputWrapper> destination)[] outputDependencies)[]
        _renderModuleProcessing
            = Array.Empty<(IRenderModule, (IRenderInput, Action<IRenderInput>)[],
                (Func<IRenderOutputWrapper>, Action<IRenderOutputWrapper> )[])>();

    private (IRenderInput, int[] dependencyIndices)[] _renderInputProcessing = Array.Empty<(IRenderInput, int[])>();

    public RenderWorker(IRenderInputManager inputManager, IRenderManager renderManager, IVulkanEngine vulkanEngine)
    {
        _workerThread = new Thread(Work);
        _renderInputManager = inputManager;
        _renderManager = renderManager;
        _vulkanEngine = vulkanEngine;
    }

    private void Work()
    {
        Initialize();
        WorkerLoop();
    }

    private void WorkerLoop()
    {
        while (!_stopRequested)
        {
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
        BuildInputProcessing();

        FlattenOutputProviders();

        var renderModuleGraph = BuildRenderModuleGraph();

        BuildModuleProcessing(renderModuleGraph.TopologicalSort().ToArray());
    }

    private void BuildModuleProcessing(IReadOnlyList<Identification> moduleOrder)
    {
        _renderModuleProcessing = new (IRenderModule,
            (IRenderInput, Action<IRenderInput>)[] inputDependencies,
            (Func<IRenderOutputWrapper> source, Action<IRenderOutputWrapper> destination)[] outputDependencies)
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
                    new (Func<IRenderOutputWrapper> source, Action<IRenderOutputWrapper> destination)[outputDependencies
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
                    Array.Empty<(Func<IRenderOutputWrapper> source, Action<IRenderOutputWrapper> destination)>();
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
        if (graph.IsDirectedAcyclicGraph())
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
    }

    /// <inheritdoc />
    public void SetInputDependency(Identification renderModuleId, Identification inputId, Action<object> callback)
    {
        if (!_renderModuleInputDependencies.ContainsKey(renderModuleId))
            _renderModuleInputDependencies.Add(renderModuleId, new Dictionary<Identification, Action<IRenderInput>>());

        _renderModuleInputDependencies[renderModuleId].Add(inputId, input => { callback(input.GetResult()); });
    }

    /// <inheritdoc />
    public void SetInputDependency<TInputResult>(Identification renderModuleId, Identification inputId,
        Action<TInputResult> callback)
    {
        if (!_renderModuleInputDependencies.ContainsKey(renderModuleId))
            _renderModuleInputDependencies.Add(renderModuleId, new Dictionary<Identification, Action<IRenderInput>>());

        _renderModuleInputDependencies[renderModuleId].Add(inputId, input =>
        {
            if (input is not IRenderInputConcreteResult<TInputResult> typedInput)
                throw new MintyCoreException($"Input is not of type TInputResult ({typeof(TInputResult)})");

            callback(typedInput.GetConcreteResulttt());
        });
    }

    /// <inheritdoc />
    public void SetOutputDependency(Identification renderModuleId, Identification outputId,
        Action<object> callback)
    {
        if (!_renderModuleOutputDependencies.ContainsKey(renderModuleId))
            _renderModuleOutputDependencies.Add(renderModuleId,
                new Dictionary<Identification, Action<IRenderOutputWrapper>>());


        _renderModuleOutputDependencies[renderModuleId]
            .Add(outputId, output => { callback(output.GetOutput()); });
    }

    /// <inheritdoc />
    public void SetOutputDependency<TOutputResult>(Identification renderModuleId, Identification outputId,
        Action<TOutputResult> callback)
    {
        if (!_renderModuleOutputDependencies.ContainsKey(renderModuleId))
            _renderModuleOutputDependencies.Add(renderModuleId,
                new Dictionary<Identification, Action<IRenderOutputWrapper>>());

        _renderModuleOutputDependencies[renderModuleId].Add(outputId, output =>
        {
            if (output is not IRenderOutputWrapper<TOutputResult> typedOutput)
                throw new MintyCoreException($"Output is not of type TOutputResult ({typeof(TOutputResult)})");

            callback(typedOutput.GetConcreteOutput());
        });
    }

    /// <inheritdoc />
    public void SetRenderModuleOutput(Identification renderModuleId, Identification outputId,
        Func<IRenderOutputWrapper> outputGetter)
    {
        if (!_renderModuleOutputProviders.ContainsKey(renderModuleId))
            _renderModuleOutputProviders.Add(renderModuleId,
                new Dictionary<Identification, Func<IRenderOutputWrapper>>());

        _renderModuleOutputProviders[renderModuleId].Add(outputId, outputGetter);
    }

    /// <inheritdoc />
    public void SetRenderModuleOutput<TRenderOutput>(Identification renderModuleId, Identification outputId,
        Func<IRenderOutputWrapper<TRenderOutput>> outputGetter)
    {
        if (!_renderModuleOutputProviders.ContainsKey(renderModuleId))
            _renderModuleOutputProviders.Add(renderModuleId,
                new Dictionary<Identification, Func<IRenderOutputWrapper>>());

        _renderModuleOutputProviders[renderModuleId].Add(outputId, outputGetter);
    }
}