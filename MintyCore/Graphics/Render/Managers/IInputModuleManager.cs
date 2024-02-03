using System.Collections.Generic;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

public interface IInputModuleManager
{
    /// <summary>
    /// Registers an input data module with the given id
    /// </summary>
    /// <param name="id"> The id of the module </param>
    /// <typeparam name="TModule"> The type of the module </typeparam>
    /// <remarks>Not intended to be called by user code</remarks>
    public void RegisterInputModule<TModule>(Identification id) where TModule : InputModule;
    
    IReadOnlySet<Identification> RegisteredInputModuleIds { get; }
    
    Dictionary<Identification, InputModule> CreateInputModuleInstances(out IContainer container);
    void SetModuleActive(Identification moduleTestId, bool isActive);
    void UnRegisterInputModule(Identification objectId);
    void Clear();
}