using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     Class containing all <see cref="Identification" /> related to ui
/// </summary>
public static class UiIDs
{
    /// <summary>
    ///     The <see cref="Identification" /> of the main menu root element
    /// </summary>
    public static Identification MainMenu { get; set; }

    /// <summary>
    ///     The <see cref="Identification" /> of the main menu prefab
    /// </summary>
    public static Identification MainMenuPrefab { get; internal set; }
}