using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Network;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> for <see cref="IMessage" />
/// </summary>
[Registry("message")]
[PublicAPI]
public class MessageRegistry : IRegistry
{
    /// <summary>
    ///     Numeric id of the registry/category
    /// </summary>
    public ushort RegistryId => RegistryIDs.Message;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        NetworkHandler.RemoveMessage(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Messages", LogImportance.Info, "Registry");
        ClearRegistryEvents();
        NetworkHandler.ClearMessages();
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    /// Register a <see cref="IMessage" />
    /// Used by the SourceGenerator for the <see cref="RegisterMessageAttribute"/>
    /// </summary>
    /// <param name="id">Id of the message</param>
    /// <typeparam name="TMessage">Type of the message</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterMessage<TMessage>(Identification id) where TMessage : class, IMessage, new()
    {
        NetworkHandler.AddMessage<TMessage>(id);
    }
}