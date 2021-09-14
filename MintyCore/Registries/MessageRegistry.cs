using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Network;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    public class MessageRegistry : IRegistry
    {
        public delegate void RegisterDelegate();
        public static event RegisterDelegate OnRegister = delegate {  };
        
        public ushort RegistryId => RegistryIDs.Message;
        public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();
        public void PreRegister()
        {
        }

        public void Register()
        {
            OnRegister.Invoke();
        }

        public void PostRegister()
        {
        }

        public void Clear()
        {
            OnRegister = delegate {  };
            MessageHandler.Clear();
        }

        public static Identification RegisterMessage<T>(ushort modId, string stringIdentification) where T : class ,IMessage, new()
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Message, stringIdentification);
            MessageHandler.AddMessage<T>(id);
            return id;
        }
    }
}