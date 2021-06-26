using System;
using System.Collections.Generic;
using MintyCore.Identifications;

namespace MintyCore.Registries
{
    public class TextureRegistry : IRegistry
    {
        public delegate void RegisterDelegate();
        		public static event RegisterDelegate OnRegister = delegate {  };
        
        public void PreRegister()
        {
        }

        public void Register()
        {
        }

        public void PostRegister()
        {
        }

        public void Clear()
        {
        }

        public ushort RegistryID => RegistryIDs.Texture;
        public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();
    }
}