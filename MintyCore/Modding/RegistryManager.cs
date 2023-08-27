using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
///     The manager class for all <see cref="IRegistry" />
/// </summary>
[PublicAPI]
public static class RegistryManager
{
    private static readonly Dictionary<ushort, IRegistry> _registries = new();

    private static readonly Dictionary<string, ushort> _modId = new();
    private static readonly Dictionary<string, ushort> _categoryId = new();
    private static readonly Dictionary<ushort, ushort> _categoryModOwner = new();

    //The key Identification is a identification with the mod and category id
    private static readonly Dictionary<Identification, Dictionary<string, ushort>> _objectId =
        new();

    private static readonly Dictionary<ushort, string> _reversedModId = new();
    private static readonly Dictionary<ushort, string> _reversedCategoryId = new();

    private static readonly Dictionary<Identification, Dictionary<ushort, string>> _reversedObjectId =
        new();

    private static readonly Dictionary<ushort, string> _categoryFolderName = new();
    private static readonly Dictionary<Identification, string> _objectFileName = new();

    /// <summary>
    ///     The <see cref="RegistryPhase" /> the game is currently in
    /// </summary>
    public static RegistryPhase RegistryPhase { get; internal set; } = RegistryPhase.None;

    /// <summary>
    ///     Defines in which object registry phase the game currently is in
    /// </summary>
    public static ObjectRegistryPhase ObjectRegistryPhase { get; internal set; } = ObjectRegistryPhase.None;


    internal static ushort RegisterModId(string stringIdentifier)
    {
        AssertModRegistryPhase();

        ushort modId;

        if (_modId.TryGetValue(stringIdentifier, out var value))
        {
            modId = value;
        }
        else
        {
            modId = Constants.InvalidId;
            do
            {
                modId++;
            } while (_reversedModId.ContainsKey(modId));

            _modId.Add(stringIdentifier, modId);
            _reversedModId.Add(modId, stringIdentifier);
        }

        return modId;
    }

    internal static ushort RegisterCategoryId(string stringIdentifier, string? folderName)
    {
        AssertCategoryRegistryPhase();

        ushort categoryId;

        if (_categoryId.TryGetValue(stringIdentifier, out var value))
        {
            categoryId = value;
        }
        else
        {
            categoryId = Constants.InvalidId;
            do
            {
                categoryId++;
            } while (_reversedCategoryId.ContainsKey(categoryId));

            _categoryId.Add(stringIdentifier, categoryId);
            _reversedCategoryId.Add(categoryId, stringIdentifier);
        }

        if (folderName is not null)
            _categoryFolderName.TryAdd(categoryId, folderName);

        return categoryId;
    }

    /// <summary>
    ///     Register a object id
    /// </summary>
    public static Identification RegisterObjectId(ushort modId, ushort categoryId, string stringIdentifier,
        string? fileName = null)
    {
        AssertObjectRegistryPhase();

        var modCategoryId = new Identification(modId, categoryId, Constants.InvalidId);

        if (!_objectId.ContainsKey(modCategoryId))
        {
            _objectId.Add(modCategoryId, new Dictionary<string, ushort>());
            _reversedObjectId.Add(modCategoryId, new Dictionary<ushort, string>());
        }

        Identification id;

        if (_objectId[modCategoryId].ContainsKey(stringIdentifier))
        {
            id = new Identification(modId, categoryId, _objectId[modCategoryId][stringIdentifier]);
        }
        else
        {
            ushort objectId = Constants.InvalidId;
            do
            {
                objectId++;
            } while (_reversedObjectId[modCategoryId].ContainsKey(objectId));

            id = new Identification(modId, categoryId, objectId);

            _objectId[modCategoryId].Add(stringIdentifier, objectId);
            _reversedObjectId[modCategoryId].Add(objectId, stringIdentifier);
        }


        if (fileName is null) return id;

        Logger.AssertAndThrow(_categoryFolderName.ContainsKey(categoryId),
            "An object file name is only allowed if a category folder name is defined", "ECS");
        
        
        
        var fileLocation = $"resources/{_categoryFolderName[categoryId]}/{fileName}";

        if (!ModManager.FileExists(id.Mod, fileLocation))
            Logger.WriteLog(
                $"File added as reference for id {id} at the location {fileLocation} does not exists.",
                LogImportance.Exception, "Registry");

        _objectFileName.Add(id, fileLocation);

        return id;
    }

    /// <summary>
    ///     Get the string id of a numeric mod id
    /// </summary>
    public static ReadOnlyDictionary<ushort, string> GetModIDs()
    {
        return new ReadOnlyDictionary<ushort, string>(_reversedModId);
    }

    /// <summary>
    ///     Get the string id of a numeric category id
    /// </summary>
    public static ReadOnlyDictionary<ushort, string> GetCategoryIDs()
    {
        return new ReadOnlyDictionary<ushort, string>(_reversedCategoryId);
    }

    /// <summary>
    ///     Get the string id for a object <see cref="Identification" />
    /// </summary>
    public static ReadOnlyDictionary<Identification, string> GetObjectIDs()
    {
        Dictionary<Identification, string> ids = new();

        foreach (var (modCategory, objectIds) in _objectId)
        foreach (var (stringId, numericId) in objectIds)
            ids.Add(new Identification(modCategory.Mod, modCategory.Category, numericId), stringId);

        return new ReadOnlyDictionary<Identification, string>(ids);
    }

    internal static void SetModIDs(IEnumerable<KeyValuePair<ushort, string>> ids)
    {
        foreach (var (numericId, stringId) in ids)
        {
            _modId.TryAdd(stringId, numericId);
            _reversedModId.TryAdd(numericId, stringId);
        }
    }

    internal static void SetCategoryIDs(IEnumerable<KeyValuePair<ushort, string>> ids)
    {
        foreach (var (numericId, stringId) in ids)
        {
            _categoryId.TryAdd(stringId, numericId);
            _reversedCategoryId.TryAdd(numericId, stringId);
        }
    }

    internal static void SetObjectIDs(IEnumerable<KeyValuePair<Identification, string>> ids)
    {
        foreach (var (objectId, stringId) in ids)
        {
            var categoryModId = new Identification(objectId.Mod, objectId.Category, Constants.InvalidId);

            if (!_objectId.ContainsKey(categoryModId))
                _objectId.Add(categoryModId, new Dictionary<string, ushort>());
            if (!_reversedObjectId.ContainsKey(categoryModId))
                _reversedObjectId.Add(categoryModId, new Dictionary<ushort, string>());

            if (!_objectId[categoryModId].ContainsKey(stringId))
                _objectId[categoryModId].Add(stringId, objectId.Object);

            if (!_reversedObjectId[categoryModId].ContainsKey(objectId.Object))
                _reversedObjectId[categoryModId].Add(objectId.Object, stringId);
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static string GetResourceFileName(Identification id)
    {
        return _objectFileName[id];
    }

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
    public static ushort AddRegistry<TRegistry>(ushort modId, string stringIdentifier, string? assetFolderName = null)
        where TRegistry : class, IRegistry, new()
    {
        AssertCategoryRegistryPhase();

        var categoryId = RegisterCategoryId(stringIdentifier, assetFolderName);

        _registries.Add(categoryId, new TRegistry());
        _categoryModOwner.Add(categoryId, modId);
        return categoryId;
    }

    internal static void ProcessRegistries()
    {
        AssertObjectRegistryPhase();
        foreach (var (id, registry) in _registries)
        foreach (var dependency in registry.RequiredRegistries)
            Logger.AssertAndThrow(_registries.ContainsKey(dependency),
                $"Registry'{_reversedCategoryId[id]}' depends on not present registry '{_reversedCategoryId[dependency]}'",
                "Registries");


        Queue<IRegistry> registryOrder = new(_registries.Count);

        HashSet<IRegistry> registriesToProcess = new(_registries.Values);

        while (registriesToProcess.Count > 0)
            foreach (var registry in new HashSet<IRegistry>(registriesToProcess))
            {
                var allDependenciesPresent = registry.RequiredRegistries.All(dependency =>
                    !registriesToProcess.Contains(_registries[dependency]));

                if (!allDependenciesPresent) continue;

                registryOrder.Enqueue(registry);
                registriesToProcess.Remove(registry);
            }

        ObjectRegistryPhase = ObjectRegistryPhase.Pre;
        for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            registries.Dequeue().PreRegister();

        ObjectRegistryPhase = ObjectRegistryPhase.Main;
        for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            registries.Dequeue().Register();

        ObjectRegistryPhase = ObjectRegistryPhase.Post;
        for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            registries.Dequeue().PostRegister();

        ObjectRegistryPhase = ObjectRegistryPhase.None;
        for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            registries.Dequeue().ClearRegistryEvents();
    }

    /// <summary>
    ///     Get the string id of a mod
    /// </summary>
    public static string GetModStringId(ushort modId)
    {
        return _reversedModId.TryGetValue(modId, out var stringId) ? stringId : "invalid";
    }

    /// <summary>
    ///     Get the string id of a category
    /// </summary>
    public static string GetCategoryStringId(ushort categoryId)
    {
        return _reversedCategoryId.TryGetValue(categoryId, out var stringId) ? stringId : "invalid";
    }

    /// <summary>
    ///     Get the string id of an object
    /// </summary>
    public static string GetObjectStringId(ushort modId, ushort categoryId, ushort objectId)
    {
        return _reversedObjectId.TryGetValue(new Identification(modId, categoryId, Constants.InvalidId),
                   out var modCategoryDic)
               && modCategoryDic.TryGetValue(objectId, out var stringId)
            ? stringId
            : "invalid";
    }

    /// <summary>
    ///     TryGet the numeric id for the given mod string identification
    /// </summary>
    public static bool TryGetModId(string modStringId, out ushort id)
    {
        return _modId.TryGetValue(modStringId, out id);
    }

    /// <summary>
    ///     TryGet the numeric id for the given category string identification
    /// </summary>
    public static bool TryGetCategoryId(string categoryStringId, out ushort id)
    {
        return _categoryId.TryGetValue(categoryStringId, out id);
    }

    /// <summary>
    ///     TryGet the <see cref="Identification" /> for the given object string and mod/category numeric id combination
    /// </summary>
    public static bool TryGetCategoryId(ushort modId, ushort categoryId, string categoryStringId,
        out Identification id)
    {
        if (_objectId[new Identification(modId, categoryId, Constants.InvalidId)]
            .TryGetValue(categoryStringId, out var objectId))
        {
            id = new Identification(modId, categoryId, objectId);
            return true;
        }

        id = Identification.Invalid;
        return false;
    }

    /// <summary>
    ///  TryGet the numeric id for the given object string identification
    /// </summary>
    /// <param name="modId"> numeric id of the mod</param>
    /// <param name="categoryId"> numeric id of the category</param>
    /// <param name="objectStringId"> string id of the object</param>
    /// <param name="id"> numeric id of the object</param>
    /// <returns> true if the object was found, false otherwise</returns>
    public static bool TryGetObjectId(ushort modId, ushort categoryId, string objectStringId,
        out Identification id)
    {
        if (_objectId[new Identification(modId, categoryId, Constants.InvalidId)]
            .TryGetValue(objectStringId, out var objectId))
        {
            id = new Identification(modId, categoryId, objectId);
            return true;
        }

        id = Identification.Invalid;
        return false;
    }

    /// <summary>
    ///    Get the numeric id for the given object string identification
    /// </summary>
    /// <param name="modStringId"> string id of the mod</param>
    /// <param name="categoryStringId"> string id of the category</param>
    /// <param name="objectStringId"> string id of the object</param>
    /// <param name="id"> numeric id of the object</param>
    /// <returns> true if the object was found, false otherwise</returns>
    public static bool TryGetObjectId(string modStringId, string categoryStringId, string objectStringId,
        out Identification id)
    {
        if (_modId.TryGetValue(modStringId, out var modId)
            && _categoryId.TryGetValue(categoryStringId, out var categoryId)
            && _objectId[new Identification(modId, categoryId, Constants.InvalidId)]
                .TryGetValue(objectStringId, out var objectId))
        {
            id = new Identification(modId, categoryId, objectId);
            return true;
        }

        id = Identification.Invalid;
        return false;
    }

    /// <summary>
    ///     Check if the game is in <see cref="Modding.RegistryPhase.Mods" />
    /// </summary>
    public static void AssertModRegistryPhase()
    {
        if (RegistryPhase != RegistryPhase.Mods)
            Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Mods}",
                LogImportance.Exception, "Registry");
    }

    /// <summary>
    ///     Check if the game is in <see cref="Modding.RegistryPhase.Categories" />
    /// </summary>
    public static void AssertCategoryRegistryPhase()
    {
        if (RegistryPhase != RegistryPhase.Categories)
            Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Categories}",
                LogImportance.Exception, "Registry");
    }

    /// <summary>
    ///     Check if the game is in <see cref="Modding.RegistryPhase.Objects" />
    /// </summary>
    public static void AssertObjectRegistryPhase()
    {
        if (RegistryPhase != RegistryPhase.Objects)
            Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Objects}",
                LogImportance.Exception, "Registry");
    }

    /// <summary>
    ///     Ensure that the game is in pre object registry phase
    /// </summary>
    public static void AssertPreObjectRegistryPhase()
    {
        Logger.AssertAndThrow(ObjectRegistryPhase == ObjectRegistryPhase.Pre,
            "Game is not in pre object registry phase",
            "Registry");
    }

    /// <summary>
    ///     Ensure that the game is in main object registry phase
    /// </summary>
    public static void AssertMainObjectRegistryPhase()
    {
        Logger.AssertAndThrow(ObjectRegistryPhase == ObjectRegistryPhase.Main,
            "Game is not in pre object registry phase",
            "Registry");
    }

    /// <summary>
    ///     Ensure that the game is in post object registry phase
    /// </summary>
    public static void AssertPostObjectRegistryPhase()
    {
        Logger.AssertAndThrow(ObjectRegistryPhase == ObjectRegistryPhase.Post,
            "Game is not in pre object registry phase",
            "Registry");
    }

    /// <summary>
    ///     Clear the registries and all internals
    /// </summary>
    public static void Clear(ushort[] modsToRemove)
    {
        //Check if the loaded mods are equal to modsToRemove and ClearAll
        if (modsToRemove.Length == _reversedModId.Count && modsToRemove.All(_reversedModId.ContainsKey))
        {
            ClearAll();
            return;
        }

        foreach (var registry in _registries.Values) registry.PreUnRegister();

        //Sort Registries to unload
        //Use a stack, as we sort the registries by the "normal" order, but unload them in reverse
        var toUnload = new Stack<(IRegistry, ushort)>();
        var toSort = new Dictionary<ushort, IRegistry>(_registries);
        while (toSort.Count != 0)
            foreach (var (id, registry) in new Dictionary<ushort, IRegistry>(toSort))
            {
                if (registry.RequiredRegistries.Any(required => toSort.ContainsKey(required))) continue;

                toUnload.Push((registry, id));
                toSort.Remove(id);
            }

        //Unload registries
        while (toUnload.TryPop(out var result))
        {
            var (registry, categoryId) = result;
            //Check if a registry was added by a mod which gets removed
            //The registry can be fully cleared in this case
            if (modsToRemove.Contains(_categoryModOwner[categoryId]))
            {
                registry.Clear();
                _categoryFolderName.Remove(categoryId);
                if (_reversedCategoryId.Remove(categoryId, out var stringId))
                    _categoryId.Remove(stringId);
                _categoryModOwner.Remove(categoryId);
                continue;
            }


            foreach (var modId in modsToRemove)
            {
                var modCategoryId = new Identification(modId, categoryId, Constants.InvalidId);
                if (!_objectId.TryGetValue(modCategoryId,
                        out var objectDictionary)) continue;

                foreach (var (objectStringId, objectNumericId) in objectDictionary)
                {
                    var objectId = new Identification(modCategoryId.Mod, modCategoryId.Category, objectNumericId);
                    registry.UnRegister(objectId);

                    _objectFileName.Remove(objectId);
                    _objectId[modCategoryId].Remove(objectStringId);
                    _reversedObjectId[modCategoryId].Remove(objectNumericId);
                }
            }
        }

        //Remove all mod id references
        foreach (var modId in modsToRemove)
        {
            if (_reversedModId.Remove(modId, out var stringModId)) _modId.Remove(stringModId);
        }

        foreach (var registry in _registries.Values) registry.PostUnRegister();
    }

    internal static void ClearAll()
    {
        foreach (var (_, registry) in _registries) registry.Clear();

        _registries.Clear();

        _modId.Clear();
        _reversedModId.Clear();

        _categoryId.Clear();
        _objectId.Clear();

        _reversedCategoryId.Clear();
        _reversedObjectId.Clear();

        _categoryFolderName.Clear();
        _categoryModOwner.Clear();
        _objectFileName.Clear();
    }
}

/// <summary />
public enum RegistryPhase
{
    /// <summary>
    ///     No registry active
    /// </summary>
    None,

    /// <summary>
    ///     Mod registry active
    /// </summary>
    Mods,

    /// <summary>
    ///     Category registry active
    /// </summary>
    Categories,

    /// <summary>
    ///     Object registry active
    /// </summary>
    Objects
}

/// <summary>
///     Object registration phase
/// </summary>
public enum ObjectRegistryPhase
{
    /// <summary>
    ///     No object registration active
    /// </summary>
    None = 0,

    /// <summary>
    ///     Pre object registration active
    /// </summary>
    Pre = 1,

    /// <summary>
    ///     Main object registration active
    /// </summary>
    Main = 2,

    /// <summary>
    ///     Post object registration active
    /// </summary>
    Post = 3
}