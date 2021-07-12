using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Registries
{
    public class PipelineRegistry : IRegistry
    {
        public delegate void RegisterDelegate();

        public static event RegisterDelegate OnRegister = delegate { };

        public static Identification RegisterGraphicsPipeline(ushort modId, string stringIdentifier, ref GraphicsPipelineDescription pipelineDescription)
        {
            Identification id = RegistryManager.RegisterObjectID(modId, RegistryIDs.Pipeline, stringIdentifier);
            PipelineHandler.AddGraphicsPipeline(id, ref pipelineDescription );
            return id;
        }
        
        public static Identification RegisterComputePipeline(ushort modId, string stringIdentifier, ref ComputePipelineDescription pipelineDescription)
        {
            Identification id = RegistryManager.RegisterObjectID(modId, RegistryIDs.Pipeline, stringIdentifier);
            PipelineHandler.AddComputePipeline(id, ref pipelineDescription );
            return id;
        }
        
        public void PreRegister()
        {
        }

        public void Register()
        {
            Logger.WriteLog("Registering Pipelines", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        public void PostRegister()
        {
        }

        public void Clear()
        {
            OnRegister = delegate { };
            PipelineHandler.Clear();
        }

        public ushort RegistryID => RegistryIDs.Pipeline;

        public ICollection<ushort> RequiredRegistries => new ushort[]
            {RegistryIDs.Shader, RegistryIDs.Texture, RegistryIDs.ResourceLayout};
    }
}