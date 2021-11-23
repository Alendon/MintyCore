namespace MintyCore.Utils
{
	/// <summary>
	///     Class to manage <see cref="Sdl2Window" />
	/// </summary>
	public class Window
    {
        private readonly Sdl2Window _window;

        /// <summary>
        ///     Create a new window
        /// </summary>
        public Window()
        {
            var createInfo = new WindowCreateInfo(100, 100, 960, 540, WindowState.Normal, "Techardry");

            _window = VeldridStartup.CreateWindow(ref createInfo);

            _window.KeyDown += InputHandler.KeyEvent;
            _window.KeyUp += InputHandler.KeyEvent;

            _window.MouseDown += InputHandler.MouseEvent;
            _window.MouseUp += InputHandler.MouseEvent;

            _window.MouseMove += InputHandler.MouseMoveEvent;
        }

        /// <summary>
        ///     Check if the window exists
        /// </summary>
        public bool Exists => _window.Exists;

        internal InputSnapshot PollEvents()
        {
            return _window.PumpEvents();
        }

        internal Sdl2Window GetWindow()
        {
            return _window;
        }
    }
}