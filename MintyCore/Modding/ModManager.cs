using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Modding
{
    internal static class ModManager
    {
        private static readonly Dictionary<string, HashSet<ModInfo>> _modInfos = new();

        private static WeakReference? _modLoadContext;
        private static readonly Dictionary<ushort, IMod> _loadedMods = new();

        public static IEnumerable<ModInfo> GetAvailableMods()
        {
            return from modInfos in _modInfos 
                from modInfo in modInfos.Value select modInfo;
        }

        public static void LoadMods(IEnumerable<ModInfo> mods)
        {
            RegistryManager.RegistryPhase = RegistryPhase.MODS;
            AssemblyLoadContext? modLoadContext = null;

            if (_modLoadContext != null && _modLoadContext.IsAlive)
            {
                modLoadContext = _modLoadContext.Target as AssemblyLoadContext;
            }
            
            if (modLoadContext is null)
            {
                modLoadContext = new AssemblyLoadContext("ModLoadContext", true);
                _modLoadContext = new WeakReference(modLoadContext);
            }
            
            foreach (var modInfo in mods)
            {
                IMod mod;
                ushort modId;
                if (modInfo.ModFileLocation.Length != 0)
                {
                    var modFile = new FileInfo(modInfo.ModFileLocation);
                    var modAssembly = modLoadContext.LoadFromAssemblyPath(modFile.FullName);

                    var modType = modAssembly.ExportedTypes.First(type =>
                        type.GetInterfaces().Any(i => i.GUID.Equals(typeof(IMod).GUID)));
                    mod = Activator.CreateInstance(modType) as IMod;
                    if (mod is null) continue;

                    var modDirectory = modFile.Directory?.FullName ?? throw new DirectoryNotFoundException("Mod directory not found... strange");

                    modId = RegistryManager.RegisterModId(mod.StringIdentifier, modDirectory);
                }
                else
                {
                    mod = new MintyCoreMod();
                    modId = RegistryManager.RegisterModId(mod.StringIdentifier, string.Empty);
                }

                mod.ModId = modId;
                _loadedMods.Add(modId, mod);
            }

            RegistryManager.RegistryPhase = RegistryPhase.CATEGORIES;

            foreach (var (_, mod) in _loadedMods)
            {
                mod.PreLoad();
            }
            foreach (var (_, mod) in _loadedMods)
            {
                mod.Load();
            }
            foreach (var (_, mod) in _loadedMods)
            {
                mod.PostLoad();
            }

            RegistryManager.RegistryPhase = RegistryPhase.OBJECTS;
            RegistryManager.ProcessRegistries();
            RegistryManager.RegistryPhase = RegistryPhase.NONE;
        }

        public static IEnumerable<(string modId, ModVersion modVersion)> GetLoadedMods()
        {
            return from loadedMod in _loadedMods
                select (loadedMod.Value.StringIdentifier, loadedMod.Value.ModVersion);
        }

        public static void UnloadMods()
        {
            RegistryManager.Clear();
            FreeMods();
            if (_modLoadContext is null) return;
            WaitForUnloading(_modLoadContext);
        }

        private static void FreeMods()
        {
            foreach (var (_, mod) in _loadedMods)
            {
                mod.Unload();
            }

            _loadedMods.Clear();
        }

        public static void SearchMods(IEnumerable<DirectoryInfo>? additionalModDirectories = null)
        {
            IEnumerable<DirectoryInfo> modDirs = Array.Empty<DirectoryInfo>();
            var modFolder = new DirectoryInfo($"{Directory.GetCurrentDirectory()}/mods");

            if (modFolder.Exists) modDirs = modDirs.Concat(modFolder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly));
            if (additionalModDirectories is not null) modDirs = modDirs.Concat(additionalModDirectories);
            
            {
                IMod mod = new MintyCoreMod();

                ModInfo modInfo = new(string.Empty, mod.StringIdentifier, mod.ModName, mod.ModDescription, mod.ModVersion, mod.ModDependencies, mod.ExecutionSide);
                
                if(!_modInfos.ContainsKey(modInfo.ModId)) _modInfos.Add(modInfo.ModId, new HashSet<ModInfo>());
                _modInfos[modInfo.ModId].Add(modInfo);
            }
            
            var sw = Stopwatch.StartNew();
            foreach (var dllFile in
                from modDir in modDirs
                from dllFile in modDir.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly)
                select dllFile)
            {
                if (IsModFile(dllFile,out var loadReference, out var modInfo))
                {
                    if(!_modInfos.ContainsKey(modInfo.ModId)) _modInfos.Add(modInfo.ModId, new HashSet<ModInfo>());
                    _modInfos[modInfo.ModId].Add(modInfo);
                }
                WaitForUnloading(loadReference);
            }
            sw.Stop();
            Logger.WriteLog($"Mod indexing took {sw.Elapsed}", LogImportance.INFO, "ModManager");
        }

        private static bool IsModFile(FileInfo dllFile, out WeakReference weakReference, out ModInfo modInfo)
        {
            var modLoadContext = new AssemblyLoadContext("CheckIsMod", true);
            weakReference = new WeakReference(modLoadContext);
            modInfo = default;
            
            Assembly assembly;
            Type? modType = null;
            
            try
            {
                assembly = modLoadContext.LoadFromAssemblyPath(dllFile.FullName);
            }
            catch(Exception)
            {
                modLoadContext.Unload();
                return false;
            }
            
            foreach (var exportedType in assembly.ExportedTypes)
            {
                if (exportedType.GetInterfaces().Any(x => x.GUID.Equals(typeof(IMod).GUID)))
                {
                    modType = exportedType;
                }
            }

            if (modType is null)
            {
                modLoadContext.Unload();
                return false;
            }
            
            var mod = Activator.CreateInstance(modType) as IMod;
            if (mod is null)
            {
                modLoadContext.Unload();
                return false;
            }

            modInfo = new ModInfo(dllFile.FullName, mod.StringIdentifier, mod.ModName, mod.ModDescription,
                mod.ModVersion, mod.ModDependencies, mod.ExecutionSide);
            
            modLoadContext.Unload();
            return true;
        }


        private static readonly int maxUnloadTries = 10;

        private static void WaitForUnloading(WeakReference loadContextReference)
        {
            for (var i = 0; i < maxUnloadTries && loadContextReference.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (!loadContextReference.IsAlive) return;

            Logger.WriteLog("Failed to unload assemblies", LogImportance.WARNING, "Modding");
        }

        /// <summary>
        /// Check if the given set of mods is compatible to the loaded mods
        /// </summary>
        /// <param name="infoAvailableMods"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool ModsCompatible(IEnumerable<(string modId, ModVersion version)> infoAvailableMods)
        {
            return _loadedMods.Values.All(mod => infoAvailableMods.Any(availableMod => mod.StringIdentifier.Equals(availableMod.modId) && mod.ModVersion.Compatible(availableMod.version)));
        }
    }

    
}