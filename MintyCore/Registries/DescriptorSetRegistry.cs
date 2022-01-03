using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries
{
    public class DescriptorSetRegistry : IRegistry
    {
        public ushort RegistryId => RegistryIDs.DescriptorSet;
        public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

        /// <summary />
        public delegate void RegisterDelegate();

        /// <summary />
        public static event RegisterDelegate OnRegister = delegate { };

        public static Identification RegisterDescriptorSet(ushort modId, string stringIdentifier,
            ReadOnlySpan<DescriptorSetLayoutBinding> bindings)
        {
            Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.DescriptorSet, stringIdentifier);
            DescriptorSetHandler.AddDescriptorSetLayout(id, bindings);
            return id;
        }

        public void PreRegister()
        {
        }

        public void Register()
        {
            OnRegister();
        }

        public void PostRegister()
        {
        }

        public void Clear()
        {
            DescriptorSetHandler.Clear();
            OnRegister = delegate { };
        }
    }
}