using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
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
    
    public required INetworkHandler NetworkHandler { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        NetworkHandler.RemoveMessage(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Messages", LogImportance.Info, "Registry");
        NetworkHandler.ClearMessages();
    }

    /// <summary>
    /// Register a <see cref="IMessage" />
    /// Used by the SourceGenerator for the <see cref="RegisterMessageAttribute"/>
    /// </summary>
    /// <param name="id">Id of the message</param>
    /// <typeparam name="TMessage">Type of the message</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterMessage<TMessage>(Identification id) where TMessage : class, IMessage
    {
        NetworkHandler.AddMessage<TMessage>(id);
    }

    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if(currentPhase == ObjectRegistryPhase.Main)
            NetworkHandler.UpdateMessages();
    }

    public void PostUnRegister()
    {
        NetworkHandler.UpdateMessages();
    }
}