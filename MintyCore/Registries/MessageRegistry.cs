using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Network;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    /// <summary>
    /// <see cref="IRegistry"/> for <see cref="IMessage"/>
    /// </summary>
    public class MessageRegistry : IRegistry
    {
        /// <summary />
        public delegate void RegisterDelegate();
        /// <summary />
        public static event RegisterDelegate OnRegister = delegate {  };
        
        /// <summary>
        /// Numeric id of the registry/category
        /// </summary>
        public ushort RegistryId => RegistryIDs.Message;

        /// <inheritdoc />
        public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

        /// <inheritdoc />
        public void PreRegister()
        {
        }

        /// <inheritdoc />
        public void Register()
        {
            Logger.WriteLog("Registering Messages", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        /// <inheritdoc />
        public void PostRegister()
        {
        }

        /// <inheritdoc />
        public void Clear()
        {
            Logger.WriteLog("Clearing Messages", LogImportance.INFO, "Registry");
            OnRegister = delegate {  };
            NetworkHandler.ClearMessages();
        }

        /// <summary>
        /// Register a <see cref="IMessage"/>
        /// </summary>
        public static Identification RegisterMessage<T>(ushort modId, string stringIdentification) where T : class ,IMessage, new()
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Message, stringIdentification);
            NetworkHandler.AddMessage<T>(id);
            return id;
        }
    }
}