using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.SDL2;
using Veldrid.StartupUtilities;

namespace MintyCore.Utils
{
	public class Window
	{
		Sdl2Window window;

		public Window()
		{
			WindowCreateInfo createInfo = new WindowCreateInfo( 100, 100, 960, 540, WindowState.Normal, "Techardry" );

			window = VeldridStartup.CreateWindow( ref createInfo );

			window.KeyDown += InputHandler.KeyEvent;
			window.KeyUp += InputHandler.KeyEvent;

			window.MouseDown += InputHandler.MouseEvent;
			window.MouseUp += InputHandler.MouseEvent;

			window.MouseMove += InputHandler.MouseMoveEvent;
		}

		public bool Exists => window.Exists;

		internal InputSnapshot PollEvents() => window.PumpEvents();

		internal Sdl2Window GetWindow() => window;

	}
}
