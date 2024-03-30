using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autofac;
using Autofac.Features.Metadata;
using JetBrains.Annotations;
using MintyCore.Modding.Providers;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Serilog;

namespace MintyCore.Modding.Implementations;

/// <summary>
///     The manager class for all <see cref="IRegistry" />
/// </summary>
[PublicAPI]
internal class RegistryManager : IRegistryManager
{
    private readonly Dictionary<string, ushort> _modId = new();
    private readonly Dictionary<string, ushort> _categoryId = new();
    private readonly Dictionary<ushort, ushort> _categoryModOwner = new();

    //The key Identification is a identification with the mod and category id
    private readonly Dictionary<Identification, Dictionary<string, uint>> _objectId =
        new();

    private readonly Dictionary<ushort, string> _reversedModId = new();
    private readonly Dictionary<ushort, string> _reversedCategoryId = new();

    private readonly Dictionary<Identification, Dictionary<uint, string>> _reversedObjectId =
        new();

    private readonly Dictionary<ushort, string> _categoryFolderName = new();
    private readonly Dictionary<Identification, string> _objectFileName = new();

    private readonly Dictionary<ushort, Action<ContainerBuilder>> _registryBuilders = new();

    private ILifetimeScope BeginRegistryLifetimeScope() =>
        ModManager.ModLifetimeScope.BeginLifetimeScope("registry", builder =>
        {
            foreach (var (_, builderAction) in _registryBuilders)
                builderAction(builder);
        });

    public RegistryManager(IModManager modManager)
    {
        ModManager = modManager;
    }

    private IModManager ModManager { get; }

    /// <summary>
    ///     The <see cref="RegistryPhase" /> the game is currently in
    /// </summary>
    public RegistryPhase RegistryPhase { get; set; } = RegistryPhase.None;

    /// <summary>
    ///     Defines in which object registry phase the game currently is in
    /// </summary>
    public ObjectRegistryPhase ObjectRegistryPhase { get; set; } = ObjectRegistryPhase.None;


    public ushort RegisterModId(string stringIdentifier)
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

    public ushort RegisterCategoryId(string stringIdentifier, string? folderName)
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
    public Identification RegisterObjectId(ushort modId, ushort categoryId, string stringIdentifier,
        string? fileName = null)
    {
        AssertObjectRegistryPhase();

        var modCategoryId = new Identification(modId, categoryId, Constants.InvalidId);

        if (!_objectId.ContainsKey(modCategoryId))
        {
            _objectId.Add(modCategoryId, new Dictionary<string, uint>());
            _reversedObjectId.Add(modCategoryId, new Dictionary<uint, string>());
        }

        Identification id;

        if (_objectId[modCategoryId].ContainsKey(stringIdentifier))
        {
            id = new Identification(modId, categoryId, _objectId[modCategoryId][stringIdentifier]);
        }
        else
        {
            uint objectId = Constants.InvalidId;
            do
            {
                objectId++;
            } while (_reversedObjectId[modCategoryId].ContainsKey(objectId));

            id = new Identification(modId, categoryId, objectId);

            _objectId[modCategoryId].Add(stringIdentifier, objectId);
            _reversedObjectId[modCategoryId].Add(objectId, stringIdentifier);
        }


        if (fileName is null) return id;

        if (!(_categoryFolderName.ContainsKey(categoryId)))
        {
            throw new MintyCoreException("An object file name is only allowed if a category folder name is defined");
        }

        var fileLocation = $"resources/{_categoryFolderName[categoryId]}/{fileName}";

        if (!ModManager.FileExists(id.Mod, fileLocation))
            throw new MintyCoreException(
                $"File added as reference for id {id} at the location {fileLocation} does not exists.");

        _objectFileName.Add(id, fileLocation);

        return id;
    }

    /// <summary>
    ///     Get the string id of a numeric mod id
    /// </summary>
    public ReadOnlyDictionary<ushort, string> GetModIDs()
    {
        return new ReadOnlyDictionary<ushort, string>(_reversedModId);
    }

    /// <summary>
    ///     Get the string id of a numeric category id
    /// </summary>
    public ReadOnlyDictionary<ushort, string> GetCategoryIDs()
    {
        return new ReadOnlyDictionary<ushort, string>(_reversedCategoryId);
    }

    /// <summary>
    ///     Get the string id for a object <see cref="Identification" />
    /// </summary>
    public ReadOnlyDictionary<Identification, string> GetObjectIDs()
    {
        Dictionary<Identification, string> ids = new();

        foreach (var (modCategory, objectIds) in _objectId)
        foreach (var (stringId, numericId) in objectIds)
            ids.Add(new Identification(modCategory.Mod, modCategory.Category, numericId), stringId);

        return new ReadOnlyDictionary<Identification, string>(ids);
    }

    public void SetModIDs(IEnumerable<KeyValuePair<ushort, string>> ids)
    {
        foreach (var (numericId, stringId) in ids)
        {
            _modId.TryAdd(stringId, numericId);
            _reversedModId.TryAdd(numericId, stringId);
        }
    }

    public void SetCategoryIDs(IEnumerable<KeyValuePair<ushort, string>> ids)
    {
        foreach (var (numericId, stringId) in ids)
        {
            _categoryId.TryAdd(stringId, numericId);
            _reversedCategoryId.TryAdd(numericId, stringId);
        }
    }

    public void SetObjectIDs(IEnumerable<KeyValuePair<Identification, string>> ids)
    {
        foreach (var (objectId, stringId) in ids)
        {
            var categoryModId = new Identification(objectId.Mod, objectId.Category, Constants.InvalidId);

            if (!_objectId.ContainsKey(categoryModId))
                _objectId.Add(categoryModId, new Dictionary<string, uint>());
            if (!_reversedObjectId.ContainsKey(categoryModId))
                _reversedObjectId.Add(categoryModId, new Dictionary<uint, string>());

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
    public string GetResourceFileName(Identification id)
    {
        return _objectFileName[id];
    }

    const string RegistryMetadataKey = "Registry";

    /// <inheritdoc />
    public ushort AddRegistry<TRegistry>(ushort modId, string stringIdentifier, string? assetFolderName,
        GameType applicableGameType)
        where TRegistry : class, IRegistry
    {
        AssertCategoryRegistryPhase();
        if (!MathHelper.IsBitSet((int)Engine.RegistryGameType, (int)applicableGameType)) return Constants.InvalidId;

        var categoryId = RegisterCategoryId(stringIdentifier, assetFolderName);

        _registryBuilders.Add(categoryId, builder =>
        {
            builder.RegisterType<TRegistry>().As<IRegistry>()
                .Named<TRegistry>(AutofacHelper.UnsafeSelfName)
                .WithMetadata(RegistryMetadataKey, categoryId);
        });

        _categoryModOwner.Add(categoryId, modId);
        return categoryId;
    }

    public void ProcessRegistries(string[] modObjectsToLoad)
    {
        AssertObjectRegistryPhase();

        using var scope = BeginRegistryLifetimeScope();
        var registriesWithMetadata = scope.Resolve<IEnumerable<Meta<IRegistry>>>();
        var registries = registriesWithMetadata.ToDictionary(
            meta => (ushort)(meta.Metadata[RegistryMetadataKey] ?? throw new Exception("Registry Metadata is null")),
            meta => meta.Value);

        foreach (var (id, registry) in registries)
        foreach (var dependency in registry.RequiredRegistries)
            if (!registries.ContainsKey(dependency))
            {
                throw new MintyCoreException(
                    $"Registry '{_reversedCategoryId[id]}' depends on not present registry '{_reversedCategoryId[dependency]}'");
            }


        List<ushort> registryOrder = new(registries.Count);

        HashSet<ushort> registriesToProcess = new(registries.Keys);

        while (registriesToProcess.Count > 0)
            foreach (var id in new HashSet<ushort>(registriesToProcess))
            {
                var registry = registries[id];
                var allDependenciesPresent = registry.RequiredRegistries.All(dependency =>
                    !registriesToProcess.Contains(dependency));

                if (!allDependenciesPresent) continue;

                registryOrder.Add(id);
                registriesToProcess.Remove(id);
            }

        ObjectRegistryPhase = ObjectRegistryPhase.Pre;
        foreach (var registryId in registryOrder)
        {
            var registry = registries[registryId];
            var registryStringId = _reversedCategoryId[registryId];

            Log.Debug("PreRegistering objects for registry {Registry}", registryStringId);
            registry.PreRegister(ObjectRegistryPhase.Pre);
            var preRegisterObjectProvider =
                scope.ResolveKeyed<IEnumerable<Meta<IPreRegisterProvider>>>(registryStringId);
            foreach (var provider in preRegisterObjectProvider)
            {
                var modTag = provider.Metadata[ModTag.MetadataName] as ModTag;
                if (modTag is null)
                    throw new MintyCoreException("ModTag metadata on RegistryProvider is null");

                var modId = _modId[modTag.Identifier];
                if (!modObjectsToLoad.Contains(modTag.Identifier)) continue;

                provider.Value.PreRegister(scope, modId);
            }

            registry.PostRegister(ObjectRegistryPhase.Pre);
        }

        ObjectRegistryPhase = ObjectRegistryPhase.Main;
        foreach (var registryId in registryOrder)
        {
            var registry = registries[registryId];
            var registryStringId = _reversedCategoryId[registryId];

            Log.Debug("MainRegistering objects for registry {Registry}", registryStringId);
            
            registry.PreRegister(ObjectRegistryPhase.Main);
            var mainRegisterObjectProvider =
                scope.ResolveKeyed<IEnumerable<Meta<IMainRegisterProvider>>>(registryStringId);
            foreach (var provider in mainRegisterObjectProvider)
            {
                var modTag = provider.Metadata[ModTag.MetadataName] as ModTag;
                if (modTag is null)
                    throw new MintyCoreException("ModTag metadata on RegistryProvider is null");

                var modId = _modId[modTag.Identifier];
                if (!modObjectsToLoad.Contains(modTag.Identifier)) continue;

                provider.Value.MainRegister(scope, modId);
            }

            registry.PostRegister(ObjectRegistryPhase.Main);
        }

        ObjectRegistryPhase = ObjectRegistryPhase.Post;
        foreach (var registryId in registryOrder)
        {
            var registry = registries[registryId];
            var registryStringId = _reversedCategoryId[registryId];
            
            Log.Debug("PostRegistering objects for registry {Registry}", registryStringId);

            registry.PreRegister(ObjectRegistryPhase.Post);
            var postRegisterObjectProvider =
                scope.ResolveKeyed<IEnumerable<Meta<IPostRegisterProvider>>>(registryStringId);
            foreach (var provider in postRegisterObjectProvider)
            {
                var modTag = provider.Metadata[ModTag.MetadataName] as ModTag;
                if (modTag is null)
                    throw new MintyCoreException("ModTag metadata on RegistryProvider is null");

                var modId = _modId[modTag.Identifier];
                if (!modObjectsToLoad.Contains(modTag.Identifier)) continue;

                provider.Value.PostRegister(scope, modId);
            }

            registry.PostRegister(ObjectRegistryPhase.Post);
        }

        ObjectRegistryPhase = ObjectRegistryPhase.None;
    }

    /// <summary>
    ///     Get the string id of a mod
    /// </summary>
    public string GetModStringId(ushort modId)
    {
        return _reversedModId.TryGetValue(modId, out var stringId) ? stringId : "invalid";
    }

    /// <summary>
    ///     Get the string id of a category
    /// </summary>
    public string GetCategoryStringId(ushort categoryId)
    {
        return _reversedCategoryId.TryGetValue(categoryId, out var stringId) ? stringId : "invalid";
    }

    /// <summary>
    ///     Get the string id of an object
    /// </summary>
    public string GetObjectStringId(ushort modId, ushort categoryId, uint objectId)
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
    public bool TryGetModId(string modStringId, out ushort id)
    {
        return _modId.TryGetValue(modStringId, out id);
    }

    /// <summary>
    ///     TryGet the numeric id for the given category string identification
    /// </summary>
    public bool TryGetCategoryId(string categoryStringId, out ushort id)
    {
        return _categoryId.TryGetValue(categoryStringId, out id);
    }

    /// <summary>
    ///     TryGet the <see cref="Identification" /> for the given object string and mod/category numeric id combination
    /// </summary>
    public bool TryGetCategoryId(ushort modId, ushort categoryId, string categoryStringId,
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
    public bool TryGetObjectId(ushort modId, ushort categoryId, string objectStringId,
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
    public bool TryGetObjectId(string modStringId, string categoryStringId, string objectStringId,
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
    ///     Check if the game is in <see cref="Implementations.RegistryPhase.Mods" />
    /// </summary>
    public void AssertModRegistryPhase()
    {
        if (RegistryPhase != RegistryPhase.Mods)
            throw new MintyCoreException($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Mods}");
    }

    /// <summary>
    ///     Check if the game is in <see cref="Implementations.RegistryPhase.Categories" />
    /// </summary>
    public void AssertCategoryRegistryPhase()
    {
        if (RegistryPhase != RegistryPhase.Categories)
            throw new MintyCoreException($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Categories}");
    }

    /// <summary>
    ///     Check if the game is in <see cref="Implementations.RegistryPhase.Objects" />
    /// </summary>
    public void AssertObjectRegistryPhase()
    {
        if (RegistryPhase != RegistryPhase.Objects)
            throw new MintyCoreException($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Objects}");
    }

    /// <summary>
    ///     Ensure that the game is in pre object registry phase
    /// </summary>
    public void AssertPreObjectRegistryPhase()
    {
        if (ObjectRegistryPhase != ObjectRegistryPhase.Pre)
            throw new MintyCoreException($"Game is not in the {nameof(ObjectRegistryPhase)}.{ObjectRegistryPhase.Pre}");
    }

    /// <summary>
    ///     Ensure that the game is in main object registry phase
    /// </summary>
    public void AssertMainObjectRegistryPhase()
    {
        if (ObjectRegistryPhase != ObjectRegistryPhase.Main)
            throw new MintyCoreException(
                $"Game is not in the {nameof(ObjectRegistryPhase)}.{ObjectRegistryPhase.Main}");
    }

    /// <summary>
    ///     Ensure that the game is in post object registry phase
    /// </summary>
    public void AssertPostObjectRegistryPhase()
    {
        if (ObjectRegistryPhase != ObjectRegistryPhase.Post)
            throw new MintyCoreException(
                $"Game is not in the {nameof(ObjectRegistryPhase)}.{ObjectRegistryPhase.Post}");
    }

    /// <summary>
    ///     Clear the registries and all internals
    /// </summary>
    public void Clear(ushort[] modsToRemove)
    {
        using var scope = BeginRegistryLifetimeScope();
        var registriesWithMetadata = scope.Resolve<IEnumerable<Meta<IRegistry>>>();
        var registries = registriesWithMetadata.ToDictionary(
            meta => (ushort)(meta.Metadata[RegistryMetadataKey] ?? throw new Exception("Registry Metadata is null")),
            meta => meta.Value);

        foreach (var registry in registries.Values) registry.PreUnRegister();

        //Sort Registries to unload
        //Use a stack, as we sort the registries by the "normal" order, but unload them in reverse
        var toUnload = new Stack<(IRegistry, ushort)>();
        var toSort = new Dictionary<ushort, IRegistry>(registries);
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

                    if (_objectId[modCategoryId].Count == 0)
                        _objectId.Remove(modCategoryId);
                    if (_reversedObjectId[modCategoryId].Count == 0)
                        _reversedObjectId.Remove(modCategoryId);
                }
            }

            //Check if a registry was added by a mod which gets removed
            //The registry can be fully cleared in this case and removed from the registry list
            if (!modsToRemove.Contains(_categoryModOwner[categoryId])) continue;

            _categoryFolderName.Remove(categoryId);
            if (_reversedCategoryId.Remove(categoryId, out var stringId))
                _categoryId.Remove(stringId);
            _categoryModOwner.Remove(categoryId);

            _registryBuilders.Remove(categoryId);
            registry.Clear();
        }

        //Remove all mod id references
        foreach (var modId in modsToRemove)
        {
            if (_reversedModId.Remove(modId, out var stringModId)) _modId.Remove(stringModId);
        }
    }

    public void PostUnRegister()
    {
        using var scope = BeginRegistryLifetimeScope();
        var registries = scope.Resolve<IEnumerable<IRegistry>>();
        foreach (var registry in registries) registry.PostUnRegister();
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