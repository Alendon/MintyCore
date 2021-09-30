using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Modding
{
    public static class ModManager
    {
        private static Dictionary<string, HashSet<ModInfo>> _modInfos = new();

        private static WeakReference? _modLoadContext = null;
        private static Dictionary<ushort, IMod> _loadedMods = new();

        public static IEnumerable<ModInfo> GetAvailableMods()
        {
            return from modInfos in _modInfos 
                from modInfo in modInfos.Value select modInfo;
        }

        public static void LoadMods(IEnumerable<ModInfo> mods)
        {
            RegistryManager.RegistryPhase = RegistryPhase.MODS;
            AssemblyLoadContext modLoadContext;

            if (_modLoadContext != null && _modLoadContext.IsAlive)
            {
                modLoadContext = _modLoadContext.Target as AssemblyLoadContext;
            }
            else
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

                    var modDirectory = modFile.Directory?.Name;

                    modId = RegistryManager.RegisterModId(mod.StringIdentifier, $@"mods\{modDirectory}");
                }
                else
                {
                    mod = new MintyCoreMod();
                    modId = RegistryManager.RegisterModId(mod.StringIdentifier, "Resources");
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

        public static void SearchMods()
        {
            var modDirectory = new DirectoryInfo($"{Directory.GetCurrentDirectory()}/mods");
            if (!modDirectory.Exists) return;

            {
                IMod mod = new MintyCoreMod();

                ModInfo modInfo = new(String.Empty, mod.StringIdentifier, mod.ModName, mod.ModDescription, mod.ModVersion, mod.ModDependencies, mod.ExecutionSide);
                
                if(!_modInfos.ContainsKey(modInfo.ModId)) _modInfos.Add(modInfo.ModId, new HashSet<ModInfo>());
                _modInfos[modInfo.ModId].Add(modInfo);
            }
            Stopwatch sw = Stopwatch.StartNew();
            foreach (var dllFile in
                from modDirs in modDirectory.EnumerateDirectories("*",
                    SearchOption.TopDirectoryOnly)
                from dllFile in modDirs.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly)
                select dllFile)
            {
                if (IsModFile(dllFile,out WeakReference loadReference, out ModInfo modInfo))
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
            
            IMod mod = Activator.CreateInstance(modType) as IMod;
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
            for (int i = 0; i < maxUnloadTries && loadContextReference.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (!loadContextReference.IsAlive) return;

            Logger.WriteLog($"Failed to unload assemblies", LogImportance.WARNING, "Modding");
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