using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries
{
	/// <summary>
	///     The <see cref="IRegistry" /> class for all <see cref="ResourceLayout" />
	/// </summary>
	public class ResourceLayoutRegistry : IRegistry
    {
	    /// <summary />
	    public delegate void RegisterDelegate();

	    /// <inheritdoc />
	    public ushort RegistryId => RegistryIDs.ResourceLayout;

	    /// <inheritdoc />
	    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

	    /// <inheritdoc />
	    public void Clear()
        {
	        Logger.WriteLog("Clearing ResourceLayouts", LogImportance.INFO, "Registry");
            ResourceLayoutHandler.Clear();
            OnRegister = delegate { };
        }

	    /// <inheritdoc />
	    public void PostRegister()
        {
        }

	    /// <inheritdoc />
	    public void PreRegister()
        {
        }

	    /// <inheritdoc />
	    public void Register()
        {
            Logger.WriteLog("Registering ResourceLayouts", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

	    /// <summary />
	    public static event RegisterDelegate OnRegister = delegate { };

	    /// <summary>
	    ///     Register a <see cref="ResourceLayout" />
	    /// </summary>
	    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="ResourceLayout" /></param>
	    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="ResourceLayout" /></param>
	    /// <param name="layoutDescription">
	    ///     The <see cref="ResourceLayoutDescription" /> of the <see cref="ResourceLayout" />
	    /// </param>
	    /// <returns>Generated <see cref="Identification" /> for <see cref="ResourceLayout" /></returns>
	    public static Identification RegisterResourceLayout(ushort modId, string stringIdentifier,
            ref ResourceLayoutDescription layoutDescription)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.ResourceLayout, stringIdentifier);
            ResourceLayoutHandler.AddResourceLayout(id, ref layoutDescription);
            return id;
        }
    }
}