using System;
using System.IO;
using System.Reflection;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Bootstrap {
    public class JABootstrap {
        public const int BootstrapVersion = 0;
        private static AppDomain domain;
        private static MethodInfo LoadJAMod;
        private static void Setup(UnityModManager.ModEntry modEntry) {
            domain = AppDomain.CurrentDomain;
            JAModInfo modInfo = LoadModInfo(modEntry);
            LoadJAMod = LoadMod(modInfo).GetMethod("LoadModInfo", (BindingFlags) 15420);
        }

        private static JAModInfo LoadModInfo(UnityModManager.ModEntry modEntry) {
            string modInfoPath = Path.Combine(modEntry.Path, "JAModInfo.json");
            if(!File.Exists(modInfoPath)) throw new FileNotFoundException("JAModInfo not found.");
            JAModInfo modInfo = File.ReadAllText(modInfoPath).FromJson<JAModInfo>();
            if(modInfo.BootstrapVersion > BootstrapVersion) throw new Exception("Bootstrap version is too low.");
            modInfo.ModEntry = modEntry;
            return modInfo;
        }

        public static Type LoadMod(JAModInfo modInfo) {
            if(modInfo.DependencyPath != null) {
                string dependencyPath = modInfo.DependencyRequireModPath ? Path.Combine(modInfo.ModEntry.Path, modInfo.DependencyPath) : modInfo.DependencyPath;
                foreach(string file in Directory.GetFiles(dependencyPath)) {
                    try {
                        domain.Load(AssemblyName.GetAssemblyName(file));
                    } catch (Exception e) {
                        modInfo.ModEntry.Logger.LogException(e);
                    }
                }
            }
            Assembly modAssembly = domain.Load(AssemblyName.GetAssemblyName(modInfo.AssemblyRequireModPath ? Path.Combine(modInfo.ModEntry.Path, modInfo.AssemblyPath) : modInfo.AssemblyPath));
            Type modType = modAssembly.GetType(modInfo.ClassName);
            if(modType == null) throw new TypeLoadException("Type not found.");
            Activator.CreateInstance(modType, (BindingFlags) 15420, null, new object[] { modInfo.ModEntry }, null, null);
            return modType;
        }

        public static void Load(UnityModManager.ModEntry modEntry) {
            JAModInfo modInfo = LoadModInfo(modEntry);
            LoadJAMod.Invoke(null, new object[] { modInfo });
        }
    }
}