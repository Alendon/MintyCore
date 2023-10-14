using System.Collections.Generic;
using System.Collections.ObjectModel;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// The implementation is not exposed by the DI Container. Use <see cref="IModManager.RegistryManager"/> instead.
/// </remarks>
public interface IRegistryManager
{
    /// <summary>
    ///     The <see cref="RegistryPhase" /> the game is currently in
    /// </summary>
    RegistryPhase RegistryPhase { get; set; }

    /// <summary>
    ///     Defines in which object registry phase the game currently is in
    /// </summary>
    ObjectRegistryPhase ObjectRegistryPhase { get; set; }

    ushort RegisterModId(string stringIdentifier);
    ushort RegisterCategoryId(string stringIdentifier, string? folderName);

    /// <summary>
    ///     Register a object id
    /// </summary>
    Identification RegisterObjectId(ushort modId, ushort categoryId, string stringIdentifier,
        string? fileName = null);

    /// <summary>
    ///     Get the string id of a numeric mod id
    /// </summary>
    ReadOnlyDictionary<ushort, string> GetModIDs();

    /// <summary>
    ///     Get the string id of a numeric category id
    /// </summary>
    ReadOnlyDictionary<ushort, string> GetCategoryIDs();

    /// <summary>
    ///     Get the string id for a object <see cref="Identification" />
    /// </summary>
    ReadOnlyDictionary<Identification, string> GetObjectIDs();

    void SetModIDs(IEnumerable<KeyValuePair<ushort, string>> ids);
    void SetCategoryIDs(IEnumerable<KeyValuePair<ushort, string>> ids);
    void SetObjectIDs(IEnumerable<KeyValuePair<Identification, string>> ids);

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetResourceFileName(Identification id);

    /// <summary>
    ///     Add a registry to the manager
    /// </summary>
    /// <typeparam name="TRegistry">
    ///     Type must be <see langword="class" />, <see cref="IRegistry" /> and expose a parameterless
    ///     constructor
    /// </typeparam>
    /// <param name="modId">Id of the mod adding the registry</param>
    /// <param name="stringIdentifier">String identifier of the registry/resulting categories</param>
    /// <param name="assetFolderName">Optional folder name for resource files</param>
    /// <returns></returns>
    ushort AddRegistry<TRegistry>(ushort modId, string stringIdentifier, string? assetFolderName, GameType applicableGameType)
        where TRegistry : class, IRegistry;

    void ProcessRegistries(string[] modObjectsToLoad);

    /// <summary>
    ///     Get the string id of a mod
    /// </summary>
    string GetModStringId(ushort modId);

    /// <summary>
    ///     Get the string id of a category
    /// </summary>
    string GetCategoryStringId(ushort categoryId);

    /// <summary>
    ///     Get the string id of an object
    /// </summary>
    string GetObjectStringId(ushort modId, ushort categoryId, ushort objectId);

    /// <summary>
    ///     TryGet the numeric id for the given mod string identification
    /// </summary>
    bool TryGetModId(string modStringId, out ushort id);

    /// <summary>
    ///     TryGet the numeric id for the given category string identification
    /// </summary>
    bool TryGetCategoryId(string categoryStringId, out ushort id);

    /// <summary>
    ///     TryGet the <see cref="Identification" /> for the given object string and mod/category numeric id combination
    /// </summary>
    bool TryGetCategoryId(ushort modId, ushort categoryId, string categoryStringId,
        out Identification id);

    /// <summary>
    ///  TryGet the numeric id for the given object string identification
    /// </summary>
    /// <param name="modId"> numeric id of the mod</param>
    /// <param name="categoryId"> numeric id of the category</param>
    /// <param name="objectStringId"> string id of the object</param>
    /// <param name="id"> numeric id of the object</param>
    /// <returns> true if the object was found, false otherwise</returns>
    bool TryGetObjectId(ushort modId, ushort categoryId, string objectStringId,
        out Identification id);

    /// <summary>
    ///    Get the numeric id for the given object string identification
    /// </summary>
    /// <param name="modStringId"> string id of the mod</param>
    /// <param name="categoryStringId"> string id of the category</param>
    /// <param name="objectStringId"> string id of the object</param>
    /// <param name="id"> numeric id of the object</param>
    /// <returns> true if the object was found, false otherwise</returns>
    bool TryGetObjectId(string modStringId, string categoryStringId, string objectStringId,
        out Identification id);

    /// <summary>
    ///     Check if the game is in <see cref="Modding.RegistryPhase.Mods" />
    /// </summary>
    void AssertModRegistryPhase();

    /// <summary>
    ///     Check if the game is in <see cref="Modding.RegistryPhase.Categories" />
    /// </summary>
    void AssertCategoryRegistryPhase();

    /// <summary>
    ///     Check if the game is in <see cref="Modding.RegistryPhase.Objects" />
    /// </summary>
    void AssertObjectRegistryPhase();

    /// <summary>
    ///     Ensure that the game is in pre object registry phase
    /// </summary>
    void AssertPreObjectRegistryPhase();

    /// <summary>
    ///     Ensure that the game is in main object registry phase
    /// </summary>
    void AssertMainObjectRegistryPhase();

    /// <summary>
    ///     Ensure that the game is in post object registry phase
    /// </summary>
    void AssertPostObjectRegistryPhase();

    /// <summary>
    ///     Clear the registries and all internals
    /// </summary>
    void Clear(ushort[] modsToRemove);

    void ClearAll();
}