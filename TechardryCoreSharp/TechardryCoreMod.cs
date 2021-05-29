using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Components;
using TechardryCoreSharp.Components.Common;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.Modding;
using TechardryCoreSharp.Registries;
using TechardryCoreSharp.SystemGroups;
using TechardryCoreSharp.Systems;
using TechardryCoreSharp.Systems.Common;

namespace TechardryCoreSharp
{
	public class TechardryCoreMod : IMod
	{
		public static TechardryCoreMod Instance;

		public TechardryCoreMod()
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

			RegistryIDs.Component = RegistryManager.AddRegistry<ComponentRegistry>( "component" );
			RegistryIDs.System = RegistryManager.AddRegistry<SystemRegistry>( "system" );
			RegistryIDs.Archetype = RegistryManager.AddRegistry<ArchetypeRegistry>( "archetype" );

			ComponentRegistry.OnRegister += RegisterComponents;
			SystemRegistry.OnRegister += RegisterSystems;
			ArchetypeRegistry.OnRegister += RegisterArchetypes;
		}

		void RegisterSystems()
		{
			SystemGroupIDs.Initialization = SystemRegistry.RegisterSystem<InitializationSystemGroup>( ModID, "initialization" );
			SystemGroupIDs.Simulation = SystemRegistry.RegisterSystem<SimulationSystemGroup>( ModID, "simulation" );
			SystemGroupIDs.Finalization = SystemRegistry.RegisterSystem<FinalizationSystemGroup>( ModID, "finalization" );
			SystemGroupIDs.Presentation = SystemRegistry.RegisterSystem<PresentationSystemGroup>( ModID, "presentation" );

			SystemIDs.ApplyTransform = SystemRegistry.RegisterSystem<ApplyTransformSystem>( ModID, "apply_transform" );
		}

		void RegisterComponents()
		{
			ComponentIDs.Position = ComponentRegistry.RegisterComponent<Position>( ModID, "position" );
			ComponentIDs.Rotation = ComponentRegistry.RegisterComponent<Rotation>( ModID, "rotation" );
			ComponentIDs.Scale = ComponentRegistry.RegisterComponent<Scale>( ModID, "scale" );
			ComponentIDs.Transform = ComponentRegistry.RegisterComponent<Transform>( ModID, "transform" );
		}

		void RegisterArchetypes()
		{
			ArchetypeContainer test1 = new ArchetypeContainer( new HashSet<Utils.Identification>() { ComponentIDs.Rotation, ComponentIDs.Position, ComponentIDs.Scale, ComponentIDs.Transform } );
			ArchetypeContainer test2 = new ArchetypeContainer( new HashSet<Utils.Identification>() { ComponentIDs.Rotation, ComponentIDs.Position, ComponentIDs.Scale } );

			ArchetypeIDs.Test0 = ArchetypeRegistry.RegisterArchetype( test1, ModID, "test0" );
			ArchetypeIDs.Test1 = ArchetypeRegistry.RegisterArchetype( test1, ModID, "test1" );
			ArchetypeIDs.Test2 = ArchetypeRegistry.RegisterArchetype( test2, ModID, "test2" );
		}
	}
}
