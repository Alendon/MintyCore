using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace MintyCore.Utils
{
	/// <summary>
	///     Class to manage <see cref="Sdl2Window" />
	/// </summary>
	public class Window
    {
        private readonly IWindow _window;
        private readonly IMouse _mouse;
        private readonly IKeyboard _keyboard;

        /// <summary>
        ///     Create a new window
        /// </summary>
        public Window()
        {
            var options = new WindowOptions(ViewOptions.DefaultVulkan); //(100, 100, 960, 540, WindowState.Normal, "Techardry");
            options.Size = new Vector2D<int>(960, 540);
            options.Title = "Techardry";

            _window = Silk.NET.Windowing.Window.Create(options);

            _window.Initialize();

            if (_window.VkSurface is null)
            {
                throw new MintyCoreException($"Vulkan surface was not created");
            }
            
            var inputContext = _window.CreateInput();
            _mouse = inputContext.Mice[0];
            _keyboard = inputContext.Keyboards[0];

            _keyboard.KeyDown += InputHandler.KeyDown;
            _keyboard.KeyUp += InputHandler.KeyUp;
            
            _mouse.MouseDown += InputHandler.MouseDown;
            _mouse.MouseUp += InputHandler.MouseUp;

            _mouse.MouseMove += InputHandler.MouseMove;
        }

        /// <summary>
        ///     Check if the window exists
        /// </summary>
        public bool Exists => !_window.IsClosing;

        internal void DoEvents()
        {
            _window.DoEvents();
        }

        internal IWindow GetWindow()
        {
            return _window;
        }
    }
}