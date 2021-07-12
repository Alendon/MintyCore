using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Registries
{
    public class ShaderRegistry : IRegistry
    {
        public delegate void RegisterDelegate();
        public static event RegisterDelegate OnRegister = delegate {  };
        public void PreRegister()
        {
            
        }

        public void Register()
        {
            Logger.WriteLog("Registering Shaders", LogImportance.INFO, "Registry");
            OnRegister.Invoke();
        }

        public static Identification RegisterShader(ushort modId, string stringIdentifier, string shaderName, ShaderStages shaderStage, string shaderEntryPoint = "main")
        {
            Identification shaderId =
                RegistryManager.RegisterObjectID(modId, RegistryIDs.Shader, stringIdentifier, shaderName);

            ShaderHandler.AddShader(shaderId, shaderStage, shaderEntryPoint);
            return shaderId;
        }

        public void PostRegister()
        {
           
        }

        public void Clear()
        {
            OnRegister = delegate {  };
            ShaderHandler.Clear();
        }

        public ushort RegistryID => RegistryIDs.Shader;
        public ICollection<ushort> RequiredRegistries => Array.Empty<ushort>();
    }
}