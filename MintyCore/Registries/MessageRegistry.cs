using System;
using System.Collections.Generic;
using Autofac;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Network;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> for <see cref="Message" />
/// </summary>
[Registry("message")]
[PublicAPI]
public class MessageRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Message;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

    /// <summary/>
    public required INetworkHandler NetworkHandler { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        NetworkHandler.RemoveMessage(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Log.Information("Clearing Messages");
        NetworkHandler.ClearMessages();
    }

    /// <summary>
    /// Register a <see cref="Message" />
    /// Used by the SourceGenerator for the <see cref="RegisterMessageAttribute"/>
    /// </summary>
    /// <param name="id">Id of the message</param>
    /// <typeparam name="TMessage">Type of the message</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterMessage<TMessage>(Identification id) where TMessage : Message
    {
        NetworkHandler.AddMessage<TMessage>(id);
    }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterUnconnectedMessage<TMessage>(Identification id) where TMessage : UnconnectedMessage
    {
        NetworkHandler.AddUnconnectedMessage<TMessage>(id);
    }

    /// <inheritdoc />
    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if (currentPhase == ObjectRegistryPhase.Main)
            NetworkHandler.UpdateMessages();
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
        NetworkHandler.UpdateMessages();
    }
}