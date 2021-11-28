using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;

namespace MintyCore.Utils
{
    /// <summary>
    ///     Class to manage user input
    /// </summary>
    public static class InputHandler
    {
        private static readonly Dictionary<Key, bool> _keyDown = new();
        private static readonly Dictionary<MouseButton, bool> _mouseDown = new();
        private static IMouse? _mouse;
        private static IKeyboard? _keyboard;

        /// <summary>
        ///     Get the current MousePosition
        /// </summary>
        public static Vector2 MousePosition { get; private set; }

        private static Vector2 _lastMousePos;

        /// <summary>
        ///     Get the current MouseDelta
        /// </summary>
        public static Vector2 MouseDelta => MousePosition - _lastMousePos;

        internal static void Setup(IMouse mouse, IKeyboard keyboard)
        {
            _mouse = mouse;
            _keyboard = keyboard;
            
            var supportedKeys = keyboard.SupportedKeys;
            _keyDown.EnsureCapacity(supportedKeys.Count);
            foreach (var key in supportedKeys)
            {
                _keyDown.Add(key, false);
            }

            var supportedButtons = mouse.SupportedButtons;
            _mouseDown.EnsureCapacity(supportedButtons.Count);
            foreach (var button in supportedButtons)
            {
                _mouseDown.Add(button, false);   
            }
        }

        internal static void MouseMoveEvent(IMouse mouse)
        {
            MousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);
        }

        /// <summary>
        ///     Get the current <see cref="KeyEvent" /> for <see cref="Key" />
        /// </summary>
        public static bool GetKeyDown(Key key)
        {
            return _keyDown[key];
        }

        /// <summary>
        ///     Get the current <see cref="MouseEvent" /> for <see cref="MouseButton" />
        /// </summary>
        public static bool GetMouseDown(MouseButton mouseButton)
        {
            return _mouseDown[mouseButton];
        }

        internal static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            _keyDown[arg2] = true;
        }

        internal static void KeyUp(IKeyboard arg1, Key arg2, int arg3)
        {
            _keyDown[arg2] = false;
        }

        internal static void MouseDown(IMouse arg1, MouseButton arg2)
        {
            _mouseDown[arg2] = true;
        }

        internal static void MouseUp(IMouse arg1, MouseButton arg2)
        {
            _mouseDown[arg2] = false;
        }

        internal static void MouseMove(IMouse arg1, Vector2 arg2)
        {
            _lastMousePos = MousePosition;
            MousePosition = arg2;
        }
    }
    
}