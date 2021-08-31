using System;
using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	/// <summary>
	///     The <see cref="IRegistry" /> class for all <see cref="IComponent" />
	/// </summary>
	public class ComponentRegistry : IRegistry
    {
	    /// <summary />
	    public delegate void RegisterDelegate();

	    /// <inheritdoc />
	    public ushort RegistryId => RegistryIDs.Component;

	    /// <inheritdoc />
	    public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();

	    /// <inheritdoc />
	    public void Clear()
        {
            OnRegister = delegate { };
            ComponentManager.Clear();
        }

	    /// <inheritdoc />
	    public void PreRegister()
        {
        }

	    /// <inheritdoc />
	    public void Register()
        {
            Logger.WriteLog("Registering Components", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

	    /// <inheritdoc />
	    public void PostRegister()
        {
        }

	    /// <summary />
	    public static event RegisterDelegate OnRegister = delegate { };

	    /// <summary>
	    ///     Register the <typeparamref name="TComponent" />
	    /// </summary>
	    /// <typeparam name="TComponent">
	    ///     Type of the Component to register. Must be <see href="unmanaged" /> and
	    ///     <see cref="IComponent" />
	    /// </typeparam>
	    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <typeparamref name="TComponent" /></param>
	    /// <param name="stringIdentifier"><see cref="string" /> id of the <typeparamref name="TComponent" /></param>
	    /// <returns>Generated <see cref="Identification" /> for <typeparamref name="TComponent" /></returns>
	    public static Identification RegisterComponent<TComponent>(ushort modId, string stringIdentifier)
            where TComponent : unmanaged, IComponent
        {
            var componentId = RegistryManager.RegisterObjectId(modId, RegistryIDs.Component, stringIdentifier);
            ComponentManager.AddComponent<TComponent>(componentId);
            return componentId;
        }
    }
}