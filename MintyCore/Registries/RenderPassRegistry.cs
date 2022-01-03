using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries
{
    public class RenderPassRegistry : IRegistry
    {
        public ushort RegistryId => RegistryIDs.RenderPass;
        public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();
        /// <summary />
        public delegate void RegisterDelegate();
        /// <summary />
        public static event RegisterDelegate OnRegister = delegate { };
        
        public void PreRegister()
        {
            
        }

        public void Register()
        {
            OnRegister();
        }

        public static Identification RegisterRenderPass(ushort modId, string stringIdentifier,
            Span<AttachmentDescription> attachments, Span<SubpassDescription> subPasses,
            Span<SubpassDependency> dependencies, RenderPassCreateFlags flags = 0)
        {
            Identification id = RegistryManager.RegisterObjectId(modId, RegistryIDs.RenderPass, stringIdentifier);
            RenderPassHandler.AddRenderPass(id, attachments, subPasses, dependencies, flags);
            return id;
        }

        public void PostRegister()
        {
            
        }

        public void Clear()
        {
            RenderPassHandler.Clear();
            OnRegister = delegate {  };
        }
    }
}