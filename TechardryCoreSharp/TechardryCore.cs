using System;
using System.Diagnostics;
using System.Threading;
using TechardryCoreSharp.Render;
using TechardryCoreSharp.Utils;
using TechardryCoreSharp.Utils.JobSystem;

namespace TechardryCoreSharp
{
	public static class TechardryCore
	{

		public static GameType GameType { get; private set; }

		public static Window Window { get; private set; }

		public static VulkanEngine VulkanEngine { get; private set; }

		class TestJob : AParallelJob
		{
			public int id;

			public TestJob( int batchSize, int iterations, int id ) : base(batchSize, iterations)
			{
				this.id = id;
			}

			public override void Execute(int i)
			{

					Console.WriteLine( $"Round2" );
				
			}
		}

		static void Main( string[] args )
		{
			Init();
			Run();
			CleanUp();
		}

		private static void Init()
		{
			Window = new Window();
			VulkanEngine = new VulkanEngine();
		}

		private static void Run()
		{
			while ( Window.Exists )
			{
				Window.PollEvents();
			}
		}

		private static void CleanUp()
		{

		}
	}
}
