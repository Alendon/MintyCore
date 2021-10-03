using System;
using System.Runtime.InteropServices;

namespace MintyVeldrid
{
    public struct PushConstantDescription
    {
        internal Func<object, bool> PushConstantValidation { get; private set; }
        internal ShaderStages ShaderStages;
        internal uint Offset;
        internal uint Size;

        public void CreateDescription<T>(ShaderStages stages, uint offset = 0) where T : unmanaged
        {
            PushConstantValidation = o => o is T;
            ShaderStages = stages;
            Offset = offset;
            Size = (uint)Marshal.SizeOf<T>();
        }

        public PushConstant<T> GetPushConstant<T>() where T : unmanaged
        {
            if (!PushConstantValidation(new T()))
            {
                throw new ArgumentException($"The given type parameter {nameof(T)} is not valid");
            }

            return new PushConstant<T>(Size, ShaderStages, Offset);
        }

        public struct PushConstant<T> where T : unmanaged
        {
            internal T Value;
            internal uint Size;
            internal ShaderStages ShaderStages;
            internal uint Offset;

            public PushConstant(uint size, ShaderStages shaderStages, uint offset) : this()
            {
                Size = size;
                ShaderStages = shaderStages;
                Offset = offset;
                Value = default;
            }

            public void SetNestedValue(T value)
            {
                Value = value;
            }


        }
    }
}