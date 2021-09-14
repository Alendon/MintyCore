using System;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using Veldrid.SDL2;

namespace MintyCore.Components.Client
{
    /// <summary>
    ///     Component to track user input
    /// </summary>
    [PlayerControlled]
    public struct Input : IComponent
    {
        /// <inheritdoc />
        public byte Dirty { get; set; }

        /// <summary>
        ///     <see cref="Identification" /> of the <see cref="Input" /> Component
        /// </summary>
        public Identification Identification => ComponentIDs.Input;

        /// <summary>
        ///     <see cref="KeyAction" /> with the current state of moving forward
        /// </summary>
        public KeyAction Forward;

        /// <summary>
        ///     <see cref="KeyAction" /> with the current state of moving backward
        /// </summary>
        public KeyAction Backward;

        /// <summary>
        ///     <see cref="KeyAction" /> with the current state of moving left
        /// </summary>
        public KeyAction Left;

        /// <summary>
        ///     <see cref="KeyAction" /> with the current state of moving right
        /// </summary>
        public KeyAction Right;

        /// <summary>
        ///     <see cref="KeyAction" /> with the current state of moving up
        /// </summary>
        public KeyAction Up;

        /// <summary>
        ///     <see cref="KeyAction" /> with the current state of moving down
        /// </summary>
        public KeyAction Down;

        /// <inheritdoc />
        public void Deserialize(DataReader reader)
        {
            Forward.SetLastKeyValid(reader.GetByte());
            Backward.SetLastKeyValid(reader.GetByte());
            Left.SetLastKeyValid(reader.GetByte());
            Right.SetLastKeyValid(reader.GetByte());
            Up.SetLastKeyValid(reader.GetByte());
            Down.SetLastKeyValid(reader.GetByte());
        }

        /// <summary>
        ///     Populate the <see cref="KeyAction" /> with default values
        /// </summary>
        public void PopulateWithDefaultValues()
        {
            Forward = new KeyAction(Key.W, KeyModifiersState.DONT_CARE, ModifierKeys.None);
            Backward = new KeyAction(Key.S, KeyModifiersState.DONT_CARE, ModifierKeys.None);
            Left = new KeyAction(Key.A, KeyModifiersState.DONT_CARE, ModifierKeys.None);
            Right = new KeyAction(Key.D, KeyModifiersState.DONT_CARE, ModifierKeys.None);
            Up = new KeyAction(Key.E, KeyModifiersState.DONT_CARE, ModifierKeys.None);
            Down = new KeyAction(Key.Q, KeyModifiersState.DONT_CARE, ModifierKeys.None);
        }

        /// <inheritdoc />
        public void Serialize(DataWriter writer)
        {
            writer.Put(Forward.NumLastKeyValid);
            writer.Put(Backward.NumLastKeyValid);
            writer.Put(Left.NumLastKeyValid);
            writer.Put(Right.NumLastKeyValid);
            writer.Put(Up.NumLastKeyValid);
            writer.Put(Down.NumLastKeyValid);
        }

        /// <summary>
        ///     Does nothing in this component
        /// </summary>
        public void IncreaseRefCount()
        {
        }

        /// <summary>
        ///     Does nothing in this component
        /// </summary>
        public void DecreaseRefCount()
        {
        }
    }

    /// <summary>
    ///     Holds all relevant data for input
    /// </summary>
    public struct KeyAction
    {
        private readonly KeyModifiersState _keyModifiersState;
        private readonly ModifierKeys _modifierKeys;
        private byte _numLastKeyValid;

        /// <summary>
        ///     Create a new <see cref="KeyAction" />
        /// </summary>
        /// <param name="key">Which <see cref="Key" /> need to be pressed</param>
        /// <param name="keyModifiersState">How <see cref="ModifierKeys" /> will behave</param>
        /// <param name="modifierKeys">Which <see cref="ModifierKeys" /> to use</param>
        public KeyAction(Key key, KeyModifiersState keyModifiersState, ModifierKeys modifierKeys) : this()
        {
            Key = key;
            _keyModifiersState = keyModifiersState;
            _modifierKeys = modifierKeys;
        }

        internal KeyAction(bool lastKeyValid)
        {
            Key = default;
            _keyModifiersState = default;
            _modifierKeys = default;
            _numLastKeyValid = lastKeyValid ? (byte)1 : (byte)0;
        }

        internal KeyAction(byte lastKeyValid)
        {
            Key = default;
            _keyModifiersState = default;
            _modifierKeys = default;
            _numLastKeyValid = lastKeyValid;
        }

        private bool ValidKeyPress(KeyEvent keyEvent)
        {
            if (!keyEvent.Down) return false;
            if (keyEvent.Key != Key) return false;
            switch (_keyModifiersState)
            {
                case KeyModifiersState.DONT_CARE: return true;
                case KeyModifiersState.AT_LEAST: return (keyEvent.Modifiers & _modifierKeys) == _modifierKeys;
                case KeyModifiersState.STRICT: return keyEvent.Modifiers == _modifierKeys;
                case KeyModifiersState.NO_OTHER: return (~_modifierKeys & keyEvent.Modifiers) == 0;
                default: throw new ApplicationException("Invalid Key Modifier State");
            }
        }

        /// <summary>
        ///     Apply a <see cref="KeyEvent" /> to update the internal state
        /// </summary>
        /// <param name="keyEvent"></param>
        /// <returns>True if the state has changed</returns>
        public bool ApplyKeyPress(KeyEvent keyEvent)
        {
            var lastValid = NumLastKeyValid;
            NumLastKeyValid = ValidKeyPress(keyEvent) ? (byte)1 : (byte)0;
            return lastValid != NumLastKeyValid;
        }

        /// <summary>
        ///     Force set the <see cref="LastKeyValid" /> value
        /// </summary>
        public void SetLastKeyValid(bool valid)
        {
            NumLastKeyValid = valid ? (byte)1 : (byte)0;
        }

        /// <summary>
        ///     Force set the <see cref="LastKeyValid" /> value
        /// </summary>
        public void SetLastKeyValid(byte valid)
        {
            NumLastKeyValid = valid;
        }
        
        /// <summary>
        /// Represents whether the last key press was valid or not as a numeric value
        /// </summary>
        public byte NumLastKeyValid { get => _numLastKeyValid; set => _numLastKeyValid = value; }

        /// <summary>
        ///     Check if the last input state is a valid press
        /// </summary>
        public bool LastKeyValid => NumLastKeyValid != 0;

        /// <summary>
        ///     Which <see cref="Key" /> is tracked
        /// </summary>
        public Key Key { get; }
    }

    /// <summary>
    ///     Enum of how <see cref="KeyModifiersState" /> behave
    /// </summary>
    public enum KeyModifiersState
    {
        /// <summary>
        ///     equal to ignore, the <see cref="KeyModifiersState" /> will not be used
        /// </summary>
        DONT_CARE,

        /// <summary>
        ///     The input <see cref="KeyModifiersState" /> have to match exactly
        /// </summary>
        STRICT,

        /// <summary>
        ///     The input <see cref="KeyModifiersState" /> needs to have at least the used <see cref="KeyModifiersState" />
        /// </summary>
        AT_LEAST,

        /// <summary>
        ///     No other input <see cref="KeyModifiersState" /> are allowed. The count of allowed <see cref="KeyModifiersState" />
        ///     is not important
        /// </summary>
        NO_OTHER
    }
}