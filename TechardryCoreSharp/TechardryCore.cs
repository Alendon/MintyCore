using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.Registries;
using TechardryCoreSharp.Render;
using TechardryCoreSharp.SystemGroups;
using TechardryCoreSharp.Utils;
using TechardryCoreSharp.Utils.JobSystem;

namespace TechardryCoreSharp
{
	public static class TechardryCore
	{
		public static GameType GameType { get; private set; }

		public static Window Window { get; private set; }

		public static VulkanEngine VulkanEngine { get; private set; }

		private static Stopwatch _tickTimeWatch = new Stopwatch();

		public static double DeltaTime { get; private set; }

		static void Main( string[] args )
		{
			Init();
			Run();
			CleanUp();
		}
		static TechardryCoreMod mod = new TechardryCoreMod();

		private static void Init()
		{
			JobManager.Start();
			Window = new Window();
			VulkanEngine = new VulkanEngine();
			VulkanEngine.Setup();

			//Temporary until a proper mod loader is ready
			RegistryManager.RegistryPhase = true;
			mod.Register( RegistryManager.RegisterModID( "techardry_core" ) );
			RegistryManager.ProcessRegistries();
		}

		private static void SetDeltaTime()
		{
			_tickTimeWatch.Stop();
			DeltaTime = _tickTimeWatch.Elapsed.TotalMilliseconds;
			_tickTimeWatch.Restart();
		}


		private static void Run()
		{
			World world = new World();

			while ( Window.Exists )
			{
				SetDeltaTime();
				world.Tick();
				Window.PollEvents();
			}
		}

		private static void CleanUp()
		{
			JobManager.Stop();
		}
	}
}
