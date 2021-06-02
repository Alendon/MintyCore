using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Registries
{
    public static partial class RegistryIDs
    {
        public static ushort Component { get; internal set; }
        public static ushort System { get; internal set; }
        public static ushort Archetype { get; internal set; }

        public static ushort Shader { get; internal set; }
        public static ushort Pipeline { get; internal set; }
        public static ushort Texture { get; internal set; }
        public static ushort Material { get; internal set; }

        public static ushort Mesh { get; internal set; }
    }
}