using System;
using System.Collections.Generic;
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
        Logger.WriteLog("Clearing Messages", LogImportance.INFO, "Registry");
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
    ///     Register a <see cref="IMessage" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    [Obsolete]
    public static Identification RegisterMessage<T>(ushort modId, string stringIdentification)
        where T : class, IMessage, new()
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Message, stringIdentification);
        NetworkHandler.AddMessage<T>(id);
        return id;
    }

    [RegisterMethod(ObjectRegistryPhase.MAIN)]
    public static void RegisterMessage<TMessage>(Identification id) where TMessage : class, IMessage, new()
    {
        NetworkHandler.AddMessage<TMessage>(id);
    }

    /// <summary>
    ///     Override a previously registered message
    ///     Call this at <see cref="OnPostRegister" />
    /// </summary>
    /// <param name="messageId">Id of the message</param>
    /// <typeparam name="T">Type of the message to override</typeparam>
    [RegisterMethod(ObjectRegistryPhase.POST, RegisterMethodOptions.UseExistingId)]
    public static void SetMessage<T>(Identification messageId) where T : class, IMessage, new()
    {
        RegistryManager.AssertPostObjectRegistryPhase();
        NetworkHandler.SetMessage<T>(messageId);
    }
}