using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Components;
using TechardryCoreSharp.Components.Common;
using TechardryCoreSharp.Modding;
using TechardryCoreSharp.Registries;

namespace TechardryCoreSharp
{
	class TechardryCoreMod : IMod
	{
		public static TechardryCoreMod Instance;

		TechardryCoreMod()
		{
			Instance = this;
		}

		public ushort ModID
		{
			get;
			private set;
		}

		public string StringIdentifier => "techardry_core";

		public void Register( ushort modID )
		{
			ModID = modID;

			ComponentRegistry.onRegister += RegisterComponents;
		}

		void RegisterComponents()
		{
			ComponentIDs.Position = ComponentRegistry.RegisterComponent<Position>( ModID, "position" );
			ComponentIDs.Rotation = ComponentRegistry.RegisterComponent<Rotation>( ModID, "rotation" );
			ComponentIDs.Scale = ComponentRegistry.RegisterComponent<Scale>( ModID, "scale" );
			ComponentIDs.Transform = ComponentRegistry.RegisterComponent<Transform>( ModID, "transform" );

		}
	}
}
