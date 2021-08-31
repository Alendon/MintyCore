using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    //TODO Add Try Get
    /// <summary>
    ///     The manager class for all <see cref="IRegistry" />
    /// </summary>
    public static class RegistryManager
    {
        private static readonly Dictionary<ushort, IRegistry> _registries = new();

        private static readonly Dictionary<string, ushort> _modId = new();
        private static readonly Dictionary<string, ushort> _categoryId = new();

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

            if (_modId.ContainsKey(stringIdentifier)) return _modId[stringIdentifier];

            ushort modId = Constants.InvalidId;
            do
            {
                modId++;
            } while (_reversedModId.ContainsKey(modId));

            _modId.Add(stringIdentifier, modId);
            _reversedModId.Add(modId, stringIdentifier);
            _modFolderName.Add(modId, folderName);

            return modId;
        }

        internal static ushort RegisterCategoryId(string stringIdentifier, string? folderName)
        {
            AssertCategoryRegistryPhase();

            if (_categoryId.ContainsKey(stringIdentifier)) return _categoryId[stringIdentifier];

            ushort categoryId = Constants.InvalidId;
            do
            {
                categoryId++;
            } while (_reversedCategoryId.ContainsKey(categoryId));

            _categoryId.Add(stringIdentifier, categoryId);
            _reversedCategoryId.Add(categoryId, stringIdentifier);
            if (folderName is not null) _categoryFolderName.Add(categoryId, folderName);

            return categoryId;
        }

        internal static Identification RegisterObjectId(ushort modId, ushort categoryId, string stringIdentifier,
            string? fileName = null)
        {
            AssertObjectRegistryPhase();

            var modCategoryId = new Identification(modId, categoryId, Constants.InvalidId);

            if (!_objectId.ContainsKey(modCategoryId))
            {
                _objectId.Add(modCategoryId, new Dictionary<string, uint>());
                _reversedObjectId.Add(modCategoryId, new Dictionary<uint, string>());
            }

            if (_objectId[modCategoryId].ContainsKey(stringIdentifier))
                return new Identification(modId, categoryId, _objectId[modCategoryId][stringIdentifier]);

            uint objectId = Constants.InvalidId;
            do
            {
                objectId++;
            } while (_reversedObjectId[modCategoryId].ContainsKey(objectId));

            Identification id = new(modId, categoryId, objectId);

            _objectId[modCategoryId].Add(stringIdentifier, objectId);
            _reversedObjectId[modCategoryId].Add(objectId, stringIdentifier);
            if (fileName is not null)
            {
                if (!_categoryFolderName.ContainsKey(categoryId))
                    throw new ArgumentException(
                        "An object file name is only allowed if a category folder name is defined");

                var fileLocation = $@".\{_modFolderName[modId]}\Resources\{_categoryFolderName[categoryId]}\{fileName}";

                if (!File.Exists(fileLocation))
                    Logger.WriteLog(
                        $"File added as reference for id {id} at the location {fileLocation} does not exists.",
                        LogImportance.EXCEPTION, "Registry");

                _objectFileName.Add(id,
                    $@".\{_modFolderName[modId]}\Resources\{_categoryFolderName[categoryId]}\{fileName}");
            }

            return id;
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
        /// <param name="assetFolderName">Optional folder name for ressource files</param>
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
            foreach (var registry in _registries)
            foreach (var dependency in registry.Value.RequiredRegistries)
                if (!_registries.ContainsKey(dependency))
                    throw new Exception(
                        $"Registry'{_reversedCategoryId[registry.Key]}' depends on not present registry '{_reversedCategoryId[dependency]}'");


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