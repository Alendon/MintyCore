using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace TechardryCoreSharp.Utils
{
	public class Window
	{
		Sdl2Window window;

		public Window()
		{
			WindowCreateInfo createInfo = new WindowCreateInfo( 100, 100, 960, 540, Veldrid.WindowState.Normal, "Techardry" );
			window = VeldridStartup.CreateWindow( ref createInfo );
		}

		public bool Exists => window.Exists;

		internal void PollEvents() => window.PumpEvents();

		internal Sdl2Window GetWindow() => window;
	}
}
