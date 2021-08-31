using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Registries
{
    /// <summary>
    ///     The <see cref="IRegistry" /> class for all <see cref="Pipeline" />
    /// </summary>
    public class PipelineRegistry : IRegistry
    {
        /// <summary />
        public delegate void RegisterDelegate();

        /// <inheritdoc />
        public void PreRegister()
        {
        }

        /// <inheritdoc />
        public void Register()
        {
            Logger.WriteLog("Registering Pipelines", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        /// <inheritdoc />
        public void PostRegister()
        {
        }

        /// <inheritdoc />
        public void Clear()
        {
            OnRegister = delegate { };
            PipelineHandler.Clear();
        }

        /// <inheritdoc />
        public ushort RegistryId => RegistryIDs.Pipeline;

        /// <inheritdoc />
        public ICollection<ushort> RequiredRegistries => new[]
            { RegistryIDs.Shader, RegistryIDs.Texture, RegistryIDs.ResourceLayout };

        /// <summary />
        public static event RegisterDelegate OnRegister = delegate { };

        /// <summary>
        ///     Register a graphics <see cref="Pipeline" />
        /// </summary>
        /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
        /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
        /// <param name="pipelineDescription">The <see cref="GraphicsPipelineDescription" /> for the pipeline to create</param>
        /// <returns>Generated <see cref="Identification" /> for <see cref="Pipeline" /></returns>
        public static Identification RegisterGraphicsPipeline(ushort modId, string stringIdentifier,
            ref GraphicsPipelineDescription pipelineDescription)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
            PipelineHandler.AddGraphicsPipeline(id, ref pipelineDescription);
            return id;
        }

        /// <summary>
        ///     Register a graphics <see cref="Pipeline" />
        /// </summary>
        /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
        /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
        /// <param name="pipelineDescription">The <see cref="GraphicsPipelineDescription" /> for the pipeline to create</param>
        /// <returns>Generated <see cref="Identification" /> for <see cref="Pipeline" /></returns>
        public static Identification RegisterGraphicsPipeline(ushort modId, string stringIdentifier,
            GraphicsPipelineDescription pipelineDescription)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
            PipelineHandler.AddGraphicsPipeline(id, ref pipelineDescription);
            return id;
        }

        /// <summary>
        ///     Register a compute <see cref="Pipeline" />
        /// </summary>
        /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
        /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
        /// <param name="pipelineDescription">The <see cref="ComputePipelineDescription" /> for the pipeline to create</param>
        /// <returns>Generated <see cref="Identification" /> for <see cref="Pipeline" /></returns>
        public static Identification RegisterComputePipeline(ushort modId, string stringIdentifier,
            ref ComputePipelineDescription pipelineDescription)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
            PipelineHandler.AddComputePipeline(id, ref pipelineDescription);
            return id;
        }

        /// <summary>
        ///     Register a compute <see cref="Pipeline" />
        /// </summary>
        /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Pipeline" /></param>
        /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Pipeline" /></param>
        /// <param name="pipelineDescription">The <see cref="ComputePipelineDescription" /> for the pipeline to create</param>
        /// <returns>Generated <see cref="Identification" /> for <see cref="Pipeline" /></returns>
        public static Identification RegisterComputePipeline(ushort modId, string stringIdentifier,
            ComputePipelineDescription pipelineDescription)
        {
            var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Pipeline, stringIdentifier);
            PipelineHandler.AddComputePipeline(id, ref pipelineDescription);
            return id;
        }
    }
}