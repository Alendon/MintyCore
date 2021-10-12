using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using MintyCore.Utils;

namespace MintyCore.Registries
{

    /// <summary>
    ///     The manager class for all <see cref="IRegistry" />
    /// </summary>
    public static class RegistryManager
    {
        private static readonly Dictionary<ushort, IRegistry> _registries = new();

        private static readonly Dictionary<string, ushort> _modId = new();
        private static readonly Dictionary<string, ushort> _categoryId = new();

        //The key Identification is a identification with the mod and category id
        private static readonly Dictionary<Identification, Dictionary<string, uint>> _objectId =
            new();

        private static readonly Dictionary<ushort, string> _reversedModId = new();
        private static readonly Dictionary<ushort, string> _reversedCategoryId = new();

        private static readonly Dictionary<Identification, Dictionary<uint, string>> _reversedObjectId =
            new();

        private static readonly Dictionary<ushort, string> _modFolderName = new();
        private static readonly Dictionary<ushort, string> _categoryFolderName = new();
        private static readonly Dictionary<Identification, string> _objectFileName = new();

        /// <summary>
        ///     The <see cref="RegistryPhase" /> the game is currently in
        /// </summary>
        public static RegistryPhase RegistryPhase { get; internal set; } = RegistryPhase.NONE;


        internal static ushort RegisterModId(string stringIdentifier, string folderName)
        {
            AssertModRegistryPhase();

            ushort modId;

            if (_modId.ContainsKey(stringIdentifier))
            {
                modId = _modId[stringIdentifier];
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

            if (!_modFolderName.ContainsKey(modId))
                _modFolderName.Add(modId, folderName);

            return modId;
        }

        internal static ushort RegisterCategoryId(string stringIdentifier, string? folderName)
        {
            AssertCategoryRegistryPhase();

            ushort categoryId;

            if (_categoryId.ContainsKey(stringIdentifier))
            {
                categoryId = _categoryId[stringIdentifier];
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

            if (folderName is not null && !_categoryFolderName.ContainsKey(categoryId))
                _categoryFolderName.Add(categoryId, folderName);

            return categoryId;
        }

        /// <summary>
        /// Register a object id
        /// </summary>
        public static Identification RegisterObjectId(ushort modId, ushort categoryId, string stringIdentifier,
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
            
            if (!_categoryFolderName.ContainsKey(categoryId))
                throw new ArgumentException(
                    "An object file name is only allowed if a category folder name is defined");

            var fileLocation = _modFolderName[modId].Length != 0 ? $@"{_modFolderName[modId]}\Resources\{_categoryFolderName[categoryId]}\{fileName}"
                : $@"{Directory.GetCurrentDirectory()}\EngineResources\{_categoryFolderName[categoryId]}\{fileName}";

            if (!File.Exists(fileLocation))
                Logger.WriteLog(
                    $"File added as reference for id {id} at the location {fileLocation} does not exists.",
                    LogImportance.EXCEPTION, "Registry");

            _objectFileName.Add(id, fileLocation);

            return id;
        }
        
        /// <summary>
        /// Get the string id of a numeric mod id
        /// </summary>
        public static ReadOnlyDictionary<ushort, string> GetModIDs()
        {
            return new ReadOnlyDictionary<ushort, string>(_reversedModId);
        }

        /// <summary>
        /// Get the string id of a numeric category id
        /// </summary>
        public static ReadOnlyDictionary<ushort, string> GetCategoryIDs()
        {
            return new ReadOnlyDictionary<ushort, string>(_reversedCategoryId);
        }

        /// <summary>
        /// Get the string id for a object <see cref="Identification"/>
        /// </summary>
        public static ReadOnlyDictionary<Identification, string> GetObjectIDs()
        {
            Dictionary<Identification, string> ids = new();

            foreach (var (modCategory, objectIds) in _objectId)
            {
                foreach (var (stringId, numericId) in objectIds)
                {
                    ids.Add(new Identification(modCategory.Mod, modCategory.Category, numericId), stringId);
                }
            }

            return new ReadOnlyDictionary<Identification, string>(ids);
        }

        internal static void SetModIDs(IDictionary<ushort, string> ids)
        {
            if (_modId.Count != 0)
            {
                Logger.WriteLog("Tried to set mod ids, while registry is not empty", LogImportance.EXCEPTION,
                    "Registry");
            }

            foreach (var (numericId, stringId) in ids)
            {
                if (!_modId.ContainsKey(stringId)) _modId.Add(stringId, numericId);
                if (!_reversedModId.ContainsKey(numericId)) _reversedModId.Add(numericId, stringId);
            }
        }

        internal static void SetCategoryIDs(IDictionary<ushort, string> ids)
        {
            if (_categoryId.Count != 0)
            {
                Logger.WriteLog("Tried to set category ids, while registry is not empty", LogImportance.EXCEPTION,
                    "Registry");
            }

            foreach (var (numericId, stringId) in ids)
            {
                if (!_categoryId.ContainsKey(stringId)) _categoryId.Add(stringId, numericId);
                if (!_reversedCategoryId.ContainsKey(numericId)) _reversedCategoryId.Add(numericId, stringId);
            }
        }

        internal static void SetObjectIDs(IDictionary<Identification, string> ids)
        {
            if (_objectId.Count != 0)
            {
                Logger.WriteLog("Tried to set object ids, while registry is not empty", LogImportance.EXCEPTION,
                    "Registry");
            }

            foreach (var (objectId, stringId) in ids)
            {
                var categoryModId = new Identification(objectId.Mod, objectId.Category, Constants.InvalidId);

                if (!_objectId.ContainsKey(categoryModId)) _objectId.Add(categoryModId, new Dictionary<string, uint>());
                if (!_reversedObjectId.ContainsKey(categoryModId)) _reversedObjectId.Add(categoryModId, new Dictionary<uint, string>());

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
        /// <param name="stringIdentifier">String identifier of the registry/resulting categories</param>
        /// <param name="assetFolderName">Optional folder name for resource files</param>
        /// <returns></returns>
        public static ushort AddRegistry<TRegistry>(string stringIdentifier, string? assetFolderName = null)
            where TRegistry : class, IRegistry, new()
        {
            AssertCategoryRegistryPhase();

            var categoryId = RegisterCategoryId(stringIdentifier, assetFolderName);

            _registries.Add(categoryId, new TRegistry());
            return categoryId;
        }

        internal static void ProcessRegistries()
        {
            AssertObjectRegistryPhase();
            foreach (var (id, registry) in _registries)
            foreach (var dependency in registry.RequiredRegistries)
                if (!_registries.ContainsKey(dependency))
                    throw new Exception(
                        $"Registry'{_reversedCategoryId[id]}' depends on not present registry '{_reversedCategoryId[dependency]}'");


            Queue<IRegistry> registryOrder = new(_registries.Count);

            HashSet<IRegistry> registriesToProcess = new(_registries.Values);

            while (registriesToProcess.Count > 0)
                foreach (var registry in new HashSet<IRegistry>(registriesToProcess))
                {
                    var allDependenciesPresent = true;
                    foreach (var dependency in registry.RequiredRegistries)
                        if (registriesToProcess.Contains(_registries[dependency]))
                        {
                            allDependenciesPresent = false;
                            break;
                        }

                    if (!allDependenciesPresent) continue;

                    registryOrder.Enqueue(registry);
                    registriesToProcess.Remove(registry);
                }

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
                registries.Dequeue().PreRegister();

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
                registries.Dequeue().Register();

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
                registries.Dequeue().PostRegister();
        }

        /// <summary>
        ///     Get the string id of a mod
        /// </summary>
        public static string GetModStringId(ushort modId)
        {
            return modId != 0 ? _reversedModId[modId] : "invalid";
        }

        /// <summary>
        ///     Get the string id of a category
        /// </summary>
        public static string GetCategoryStringId(ushort categoryId)
        {
            return categoryId != 0 ? _reversedCategoryId[categoryId] : "invalid";
        }

        /// <summary>
        ///     Get the string id of an object
        /// </summary>
        public static string GetObjectStringId(ushort modId, ushort categoryId, uint objectId)
        {
            if (modId == 0 || categoryId == 0 || objectId == 0) return "invalid";
            return _reversedObjectId[new Identification(modId, categoryId, Constants.InvalidId)][objectId];
        }

        /// <summary>
        /// TryGet the numeric id for the given mod string identification
        /// </summary>
        public static bool TryGetModId(string modStringId, out ushort id)
        {
            return _modId.TryGetValue(modStringId, out id);
        }
        
        /// <summary>
        /// TryGet the numeric id for the given category string identification
        /// </summary>
        public static bool TryGetCategoryId(string categoryStringId, out ushort id)
        {
            return _categoryId.TryGetValue(categoryStringId, out id);
        }
        
        /// <summary>
        /// TryGet the <see cref="Identification"/> for the given object string and mod/category numeric id combination
        /// </summary>
        public static bool TryGetCategoryId(ushort modId, ushort categoryId, string categoryStringId, out Identification id)
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
        ///     Check if the game is in <see cref="Registries.RegistryPhase.MODS" />
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertModRegistryPhase()
        {
            if (RegistryPhase != RegistryPhase.MODS)
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.MODS}",
                    LogImportance.EXCEPTION, "Registry");
        }

        /// <summary>
        ///     Check if the game is in <see cref="Registries.RegistryPhase.CATEGORIES" />
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertCategoryRegistryPhase()
        {
            if (RegistryPhase != RegistryPhase.CATEGORIES)
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.CATEGORIES}",
                    LogImportance.EXCEPTION, "Registry");
        }

        /// <summary>
        ///     Check if the game is in <see cref="Registries.RegistryPhase.OBJECTS" />
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertObjectRegistryPhase()
        {
            if (RegistryPhase != RegistryPhase.OBJECTS)
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.OBJECTS}",
                    LogImportance.EXCEPTION, "Registry");
        }

        /// <summary>
        /// Clear the registries and all internals
        /// </summary>
        public static void Clear()
        {
            foreach (var (_, registry) in _registries)
            {
                registry.Clear();
            }

            _registries.Clear();

            _modId.Clear();
            _categoryId.Clear();
            _objectId.Clear();
            _reversedModId.Clear();
            _reversedCategoryId.Clear();
            _reversedObjectId.Clear();

            _modFolderName.Clear();
            _categoryFolderName.Clear();
            _modFolderName.Clear();
        }
    }

    /// <summary />
    public enum RegistryPhase
    {
        /// <summary>
        ///     No registry active
        /// </summary>
        NONE,

        /// <summary>
        ///     Mod registry active
        /// </summary>
        MODS,

        /// <summary>
        ///     Category registry active
        /// </summary>
        CATEGORIES,

        /// <summary>
        ///     Object registry active
        /// </summary>
        OBJECTS
    }
}