using System.Collections.Generic;
using System.Numerics;
using Veldrid.SDL2;

namespace MintyCore.Utils
{
	/// <summary>
	///     Class to manage user input
	/// </summary>
	public static class InputHandler
    {
        private static readonly Dictionary<Key, KeyEvent> _keyEvents = new();
        private static readonly Dictionary<MouseButton, MouseEvent> _mouseEvents = new();

        /// <summary>
        ///     Get the current MousePosition
        /// </summary>
        public static Vector2 MousePosition { get; private set; }

        /// <summary>
        ///     Get the current MouseDelta
        /// </summary>
        public static Vector2 MouseDelta => new(MintyCore.Window.GetWindow().MouseDelta.X,
            MintyCore.Window.GetWindow().MouseDelta.Y);

        internal static void KeyEvent(KeyEvent obj)
        {
            if (_keyEvents.ContainsKey(obj.Key))
                _keyEvents[obj.Key] = obj;
            else
                _keyEvents.Add(obj.Key, obj);

            switch (obj.Key)
            {
                case Key.Space:
                {
                    if (obj.Down)
                        MintyCore.NextRenderMode();
                    break;
                }
            }
        }

        internal static void MouseEvent(MouseEvent obj)
        {
            if (_mouseEvents.ContainsKey(obj.MouseButton))
                _mouseEvents[obj.MouseButton] = obj;
            else
                _mouseEvents.Add(obj.MouseButton, obj);
        }

        internal static void MouseMoveEvent(MouseMoveEventArgs obj)
        {
            MousePosition = new Vector2(obj.MousePosition.X, obj.MousePosition.Y);
        }

        /// <summary>
        ///     Get the current <see cref="KeyEvent" /> for <see cref="Key" />
        /// </summary>
        public static KeyEvent GetKeyEvent(Key key)
        {
            if (_keyEvents.ContainsKey(key)) return _keyEvents[key];
            return new KeyEvent(key, false, ModifierKeys.None, false);
        }

        /// <summary>
        ///     Get the current <see cref="MouseEvent" /> for <see cref="MouseButton" />
        /// </summary>
        public static MouseEvent GetMouseEvent(MouseButton mouseButton)
        {
            if (_mouseEvents.ContainsKey(mouseButton)) return _mouseEvents[mouseButton];
            return new MouseEvent(mouseButton, false);
        }
    }
}