namespace MintyCore.Input;

/// <summary>
/// The result of an input action.
/// </summary>
public enum InputActionResult
{
    /// <summary>
    /// Continue processing the remaining input actions.
    /// </summary>
    Continue,
    
    /// <summary>
    ///  Stop processing the remaining input actions.
    /// </summary>
    Stop
}