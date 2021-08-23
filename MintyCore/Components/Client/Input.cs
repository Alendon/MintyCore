using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.SDL2;

namespace MintyCore.Components.Client
{
	public struct Input : IComponent
	{
		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Input;

		public KeyAction Forward;
		public KeyAction Backward;
		public KeyAction Left;
		public KeyAction Right;
		public KeyAction Up;
		public KeyAction Down;

		public void Deserialize(DataReader reader)
		{
			Forward = new KeyAction(reader.GetByte());
			Backward = new KeyAction(reader.GetByte());
			Left = new KeyAction(reader.GetByte());
			Right = new KeyAction(reader.GetByte());
			Up = new KeyAction(reader.GetByte());
			Down = new KeyAction(reader.GetByte());
		}

		public void PopulateWithDefaultValues()
		{
			Forward = new KeyAction(Key.W, KeyModifiersState.DontCare, ModifierKeys.None);
			Backward = new KeyAction(Key.S, KeyModifiersState.DontCare, ModifierKeys.None);
			Left = new KeyAction(Key.A, KeyModifiersState.DontCare, ModifierKeys.None);
			Right = new KeyAction(Key.D, KeyModifiersState.DontCare, ModifierKeys.None);
			Up = new KeyAction(Key.E, KeyModifiersState.DontCare, ModifierKeys.None);
			Down = new KeyAction(Key.Q, KeyModifiersState.DontCare, ModifierKeys.None);

		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(Forward._lastKeyValid);
			writer.Put(Backward._lastKeyValid);
			writer.Put(Left._lastKeyValid);
			writer.Put(Right._lastKeyValid);
			writer.Put(Up._lastKeyValid);
			writer.Put(Down._lastKeyValid);
		}

		public void IncreaseRefCount()
		{
		}

		public void DecreaseRefCount()
		{
		}
	}

	public struct KeyAction
	{
		private Key _key;
		private KeyModifiersState _keyModifiersState;
		private ModifierKeys _modifierKeys;
		internal byte _lastKeyValid;

		public KeyAction(Key key, KeyModifiersState keyModifiersState, ModifierKeys modifierKeys) : this()
		{
			_key = key;
			_keyModifiersState = keyModifiersState;
			_modifierKeys = modifierKeys;
		}
		public KeyAction(byte lastKeyValid)
		{
			_key = default;
			_keyModifiersState = default;
			_modifierKeys = default;
			_lastKeyValid = lastKeyValid;
		}

		bool ValidKeyPress(KeyEvent keyEvent)
		{
			if (!keyEvent.Down) return false;
			if (keyEvent.Key != _key) return false;
			switch (_keyModifiersState)
			{
				case KeyModifiersState.DontCare: return true;
				case KeyModifiersState.AtLeast: return (keyEvent.Modifiers & _modifierKeys) == _modifierKeys;
				case KeyModifiersState.Strict: return keyEvent.Modifiers == _modifierKeys;
				case KeyModifiersState.NoOther: return (~_modifierKeys & keyEvent.Modifiers) == 0;
				default: throw new ApplicationException("Invalid Key Modifier State");
			}
		}

		public void ApplyKeyPress(KeyEvent keyEvent)
		{
			_lastKeyValid = ValidKeyPress(keyEvent) ? (byte)1 : (byte)0;
		}

		public void SetLastKeyValid(bool valid)
		{
			_lastKeyValid = valid ? (byte)1 : (byte)0;
		}

		public bool LastKeyValid => _lastKeyValid != 0;

		public Key Key => _key;	


	}

	public enum KeyModifiersState
	{
		DontCare,
		Strict,
		AtLeast,
		NoOther
	}
}
