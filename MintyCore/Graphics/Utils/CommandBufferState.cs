namespace MintyCore.Graphics.Utils;

/// <summary>
/// Represents the possible states of a command buffer.
/// </summary>
public enum CommandBufferState
{
    /// <summary>
    /// The initial state of a command buffer.
    /// A command buffer is in this state when it is first created.
    /// </summary>
    Initial,
    
    /// <summary>
    /// The recording state of a command buffer.
    /// </summary>
    Recording,
    
    /// <summary>
    /// The executable state of a command buffer.
    /// </summary>
    Executable
}