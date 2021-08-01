using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    //TODO Add Try Get
    /// <summary>
    /// The manager class for all <see cref="IRegistry"/>
    /// </summary>
    public static class RegistryManager
    {
        private static Dictionary<ushort, IRegistry> _registries = new();

        private static Dictionary<string, ushort> _modID = new();
        private static Dictionary<string, ushort> _categoryID = new();

        private static Dictionary<Identification, Dictionary<string, uint>> _objectID =
            new();

        private static Dictionary<ushort, string> _reversedModID = new();
        private static Dictionary<ushort, string> _reversedCategoryID = new();

        private static Dictionary<Identification, Dictionary<uint, string>> _reversedObjectID =
            new();

        private static Dictionary<ushort, string> _modFolderName = new();
        private static Dictionary<ushort, string> _categoryFolderName = new();
        private static Dictionary<Identification, string> _objectFileName = new();

        /// <summary>
        /// The <see cref="RegistryPhase"/> the game is currently in
        /// </summary>
        public static RegistryPhase RegistryPhase { get; internal set; } = RegistryPhase.None;


        internal static ushort RegisterModID(string stringIdentifier, string folderName)
        {
            AssertModRegistryPhase();

            if (_modID.ContainsKey(stringIdentifier))
            {
                return _modID[stringIdentifier];
            }

            ushort modID = Constants.InvalidID;
            do
            {
                modID++;
            } while (_reversedModID.ContainsKey(modID));

            _modID.Add(stringIdentifier, modID);
            _reversedModID.Add(modID, stringIdentifier);
            _modFolderName.Add(modID, folderName);

            return modID;
        }

        internal static ushort RegisterCategoryID(string stringIdentifier, string? folderName)
        {
            AssertCategoryRegistryPhase();

            if (_categoryID.ContainsKey(stringIdentifier))
            {
                return _categoryID[stringIdentifier];
            }

            ushort categoryID = Constants.InvalidID;
            do
            {
                categoryID++;
            } while (_reversedCategoryID.ContainsKey(categoryID));

            _categoryID.Add(stringIdentifier, categoryID);
            _reversedCategoryID.Add(categoryID, stringIdentifier);
            if (folderName is not null)
            {
                _categoryFolderName.Add(categoryID, folderName);
            }

            return categoryID;
        }

        internal static Identification RegisterObjectID(ushort modID, ushort categoryID, string stringIdentifier,
            string? fileName = null)
        {
            AssertObjectRegistryPhase();

            Identification modCategoryID = new Identification(modID, categoryID, Constants.InvalidID);

            if (!_objectID.ContainsKey(modCategoryID))
            {
                _objectID.Add(modCategoryID, new Dictionary<string, uint>());
                _reversedObjectID.Add(modCategoryID, new Dictionary<uint, string>());
            }

            if (_objectID[modCategoryID].ContainsKey(stringIdentifier))
            {
                return new Identification(modID, categoryID, _objectID[modCategoryID][stringIdentifier]);
            }

            uint objectID = Constants.InvalidID;
            do
            {
                objectID++;
            } while (_reversedObjectID[modCategoryID].ContainsKey(objectID));

            Identification id = new(modID, categoryID, objectID);

            _objectID[modCategoryID].Add(stringIdentifier, objectID);
            _reversedObjectID[modCategoryID].Add(objectID, stringIdentifier);
            if (fileName is not null)
            {
                if (!_categoryFolderName.ContainsKey(categoryID))
                {
                    throw new ArgumentException(
                        "An object file name is only allowed if a category folder name is defined");
                }

                var fileLocation = $@".\{_modFolderName[modID]}\Resources\{_categoryFolderName[categoryID]}\{fileName}";

				if (!File.Exists(fileLocation))
				{
                    Logger.WriteLog($"File added as reference for id {id} at the location {fileLocation} does not exists.", LogImportance.EXCEPTION, "Registry");
				}

                _objectFileName.Add(id, $@".\{_modFolderName[modID]}\Resources\{_categoryFolderName[categoryID]}\{fileName}");
            }

            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetResourceFileName(Identification id)
        {
            return _objectFileName[id];
        }

        /// <summary>
        /// Add a registry to the manager
        /// </summary>
        /// <typeparam name="TRegistry">Type must be <see langword="class"/>, <see cref="IRegistry"/> and expose a parameterless constructor </typeparam>
        /// <param name="stringIdentifier">String identifier of the registry/resulting categories</param>
        /// <param name="assetFolderName">Optional folder name for ressource files</param>
        /// <returns></returns>
        public static ushort AddRegistry<TRegistry>(string stringIdentifier, string? assetFolderName = null)
            where TRegistry : class, IRegistry, new()
        {
            AssertCategoryRegistryPhase();

            var categoryId = RegisterCategoryID(stringIdentifier, assetFolderName);

            _registries.Add(categoryId, new TRegistry());
            return categoryId;
        }

        internal static void ProcessRegistries()
        {
            AssertObjectRegistryPhase();
            foreach (var registry in _registries)
            {
                foreach (var dependency in registry.Value.RequiredRegistries)
                {
                    if (!_registries.ContainsKey(dependency))
                    {
                        throw new Exception(
                            $"Registry'{_reversedCategoryID[registry.Key]}' depends on not present registry '{_reversedCategoryID[dependency]}'");
                    }
                }
            }


            Queue<IRegistry> registryOrder = new(_registries.Count);

            HashSet<IRegistry> registriesToProcess = new(_registries.Values);

            while (registriesToProcess.Count > 0)
            {
                foreach (var registry in new HashSet<IRegistry>(registriesToProcess))
                {
                    bool allDependenciesPresent = true;
                    foreach (var dependency in registry.RequiredRegistries)
                    {
                        if (registriesToProcess.Contains(_registries[dependency]))
                        {
                            allDependenciesPresent = false;
                            break;
                        }
                    }

                    if (!allDependenciesPresent)
                    {
                        continue;
                    }

                    registryOrder.Enqueue(registry);
                    registriesToProcess.Remove(registry);
                }
            }

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            {
                registries.Dequeue().PreRegister();
            }

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            {
                registries.Dequeue().Register();
            }

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            {
                registries.Dequeue().PostRegister();
            }
        }

        /// <summary>
        /// Get the string id of a mod
        /// </summary>
        public static string GetModStringID(ushort modID)
        {
            return modID != 0 ? _reversedModID[modID] : "invalid";
        }

        /// <summary>
        /// Get the string id of a category
        /// </summary>
        public static string GetCategoryStringID(ushort categoryID)
        {
            return categoryID != 0 ? _reversedCategoryID[categoryID] : "invalid";
        }

        /// <summary>
        /// Get the string id of an object
        /// </summary>
        public static string GetObjectStringID(ushort modID, ushort categoryID, uint objectID)
        {
            if (modID == 0 || categoryID == 0 || objectID == 0) return "invalid";
            return _reversedObjectID[new Identification(modID, categoryID, Constants.InvalidID)][objectID];
        }

        /// <summary>
        /// Check if the game is in <see cref="RegistryPhase.Mods"/>
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertModRegistryPhase()
        {
            if (RegistryPhase != RegistryPhase.Mods)
            {
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Mods}", LogImportance.EXCEPTION, "Registry");
            }
        }

        /// <summary>
        /// Check if the game is in <see cref="RegistryPhase.Categories"/>
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertCategoryRegistryPhase()
        {
            if (RegistryPhase != RegistryPhase.Categories)
            {
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Categories}", LogImportance.EXCEPTION, "Registry");
            }
        }

        /// <summary>
        /// Check if the game is in <see cref="RegistryPhase.Objects"/>
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertObjectRegistryPhase()
		{
            if(RegistryPhase != RegistryPhase.Objects)
			{
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Objects}", LogImportance.EXCEPTION, "Registry");
			}
		}
    }

    /// <summary/>
    public enum RegistryPhase
	{
        /// <summary>
        /// No registry active
        /// </summary>
        None,

        /// <summary>
        /// Mod registry active
        /// </summary>
        Mods,

        /// <summary>
        /// Category registry active
        /// </summary>
        Categories,

        /// <summary>
        /// Object registry active
        /// </summary>
        Objects
    }
}