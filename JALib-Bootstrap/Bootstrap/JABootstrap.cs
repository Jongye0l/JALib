using System;
using System.IO;
using System.Reflection;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Bootstrap {
    public class JABootstrap {
        public const int BootstrapVersion = 0;
        private static AppDomain domain;
        private static void Setup(UnityModManager.ModEntry modEntry) {
            domain = AppDomain.CreateDomain("JAModDomain", null, new AppDomainSetup {
                ApplicationBase = modEntry.Path
            });
            Load(modEntry);
        }
        
        public static void Load(UnityModManager.ModEntry modEntry) {
            string modInfoPath = Path.Combine(modEntry.Path, "JAModInfo.json");
            if(!File.Exists(modInfoPath)) throw new FileNotFoundException("JAModInfo not found.");
            JAModInfo modInfo = File.ReadAllText(modInfoPath).FromJson<JAModInfo>();
            if(modInfo.DependencyPath != null) {
                string dependencyPath = modInfo.DependencyRequireModPath ? Path.Combine(modEntry.Path, modInfo.DependencyPath) : modInfo.DependencyPath;
                foreach(string file in Directory.GetFiles(dependencyPath)) {
                    try {
                        domain.Load(AssemblyName.GetAssemblyName(file));
                    } catch (Exception e) {
                        modEntry.Logger.LogException(e);
                    }
                }
            }
            Assembly modAssembly = domain.Load(AssemblyName.GetAssemblyName(modInfo.AssemblyRequireModPath ? Path.Combine(modEntry.Path, modInfo.AssemblyPath) : modInfo.AssemblyPath));
            Type modType = modAssembly.GetType(modInfo.ClassName);
            if(modType == null) throw new TypeLoadException("Type not found.");
            Activator.CreateInstance(modType, modEntry);
        }
    }
}