using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Registries
{
    /// <summary>
	/// The <see cref="IRegistry"/> class for all <see cref="Shader"/>
	/// </summary>
    public class ShaderRegistry : IRegistry
    {
        /// <summary/>
        public delegate void RegisterDelegate();
        /// <summary/>
        public static event RegisterDelegate OnRegister = delegate {  };
        /// <inheritdoc/>
        public void PreRegister()
        {
            
        }

        /// <inheritdoc/>
        public void Register()
        {
            Logger.WriteLog("Registering Shaders", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        /// <summary>
        /// Register a <see cref="Shader"/>
        /// </summary>
        /// <param name="modId"><see cref="ushort"/> id of the mod registering the <see cref="Shader"/></param>
        /// <param name="stringIdentifier"><see cref="string"/> id of the <see cref="Shader"/></param>
        /// <param name="shaderName">The file name of the <see cref="Shader"/></param>
        /// <param name="shaderStage">The <see cref="ShaderStages"/> of the <see cref="Shader"/></param>
        /// <param name="shaderEntryPoint">The entry point (main method) of the <see cref="Shader"/></param>
        /// <returns>Generated <see cref="Identification"/> for <see cref="Shader"/></returns>
        public static Identification RegisterShader(ushort modId, string stringIdentifier, string shaderName, ShaderStages shaderStage, string shaderEntryPoint = "main")
        {
            Identification shaderId =
                RegistryManager.RegisterObjectID(modId, RegistryIDs.Shader, stringIdentifier, shaderName);

            ShaderHandler.AddShader(shaderId, shaderStage, shaderEntryPoint);
            return shaderId;
        }

        /// <inheritdoc/>
        public void PostRegister()
        {
           
        }

        /// <inheritdoc/>
        public void Clear()
        {
            OnRegister = delegate {  };
            ShaderHandler.Clear();
        }

        /// <inheritdoc/>
        public ushort RegistryID => RegistryIDs.Shader;
        /// <inheritdoc/>
        public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();
    }
}