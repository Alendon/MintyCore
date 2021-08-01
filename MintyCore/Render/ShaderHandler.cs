using System.Collections.Generic;
using System.IO;
using MintyCore.Registries;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Render
{
    /// <summary>
    /// The handler for all shader specific stuff. Get populated by the <see cref="ShaderRegistry"/>
    /// </summary>
    public static class ShaderHandler
    {
        private static Dictionary<Identification, Shader> _shaders = new();
        
        internal static void AddShader(Identification shaderId, ShaderStages shaderStage, string shaderEntryPoint)
        {
            string shaderFile = RegistryManager.GetResourceFileName(shaderId);
            byte[] shaderCode = File.Exists(shaderFile)
                ? File.ReadAllBytes(shaderFile)
                : throw new IOException("Shader file to load does not exists");
            
            ShaderDescription shaderDesc = new ShaderDescription()
            {
                Stage = shaderStage,
                EntryPoint = shaderEntryPoint,
                ShaderBytes = shaderCode
            };

            Shader shader = VulkanEngine.GraphicsDevice.ResourceFactory.CreateShader(shaderDesc);
            _shaders.Add(shaderId, shader);
        }

        /// <summary>
        /// Get a <see cref="Shader"/>
        /// </summary>
        /// <param name="shaderId"><see cref="Identification"/> of the <see cref="Shader"/></param>
        /// <returns></returns>
        public static Shader GetShader(Identification shaderId)
        {
            return _shaders[shaderId];
        }

        internal static void Clear()
        {
            foreach (var shader in _shaders)
            {
                shader.Value.Dispose();
            }
            _shaders.Clear();
        }
    }
}