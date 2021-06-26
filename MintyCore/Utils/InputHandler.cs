using Ara3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.SDL2;

namespace MintyCore.Utils
{
	public static class InputHandler
	{
		private static Dictionary<Key, KeyEvent> keyEvents = new();
		private static Dictionary<MouseButton, MouseEvent> mouseEvents = new();
		public static Vector2 MousePosition { get; private set;  }
		public static Vector2 MouseDelta => new Vector2(MintyCore.Window.GetWindow().MouseDelta.X, MintyCore.Window.GetWindow().MouseDelta.Y);

		internal static void KeyEvent(KeyEvent obj)
		{
			if (keyEvents.ContainsKey(obj.Key))
			{
				keyEvents[obj.Key] = obj;
			}
			else
			{
				keyEvents.Add(obj.Key, obj);
			}
		}

		internal static void MouseEvent(MouseEvent obj)
		{
			if (mouseEvents.ContainsKey(obj.MouseButton))
			{
				mouseEvents[obj.MouseButton] = obj;
			}
			else
			{
				mouseEvents.Add(obj.MouseButton, obj);
			}
		}

		internal static void MouseMoveEvent(MouseMoveEventArgs obj)
		{
			MousePosition = new Vector2(obj.MousePosition.X, obj.MousePosition.Y);
		}

		public static KeyEvent GetKeyEvent(Key key)
		{
			if (keyEvents.ContainsKey(key))
			{
				return keyEvents[key];
			}
			return new KeyEvent(key, false, ModifierKeys.None, false);
		}

		public static MouseEvent GetMouseEvent(MouseButton mouseButton)
		{
			if (mouseEvents.ContainsKey(mouseButton))
			{
				return mouseEvents[mouseButton];
			}
			return new MouseEvent(mouseButton, false);
		}

	
	}
}
