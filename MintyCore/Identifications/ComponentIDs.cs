using MintyCore.Utils;

namespace MintyCore.Identifications
{
    public static partial class ComponentIDs
    {
        public static Identification Position { get; internal set; }
        public static Identification Rotation { get; internal set; }
        public static Identification Scale { get; internal set; }
        public static Identification Transform { get; internal set; }
        
        public static Identification Renderable { get; internal set; }
		public static Identification Camera { get; internal set; }
		public static Identification Input { get; internal set; }
		public static Identification Rotator { get; internal set; }
	}
}