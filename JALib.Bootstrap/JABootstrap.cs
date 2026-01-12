using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Bootstrap;

public static class JABootstrap {
    public const int BootstrapVersion = 1;
    private static AppDomain domain;
    private static Action<JAModInfo> LoadJAMod;
    private static Task _task;
    [Obsolete] internal static Harmony harmony;
    private static JAModInfo jalibModInfo;
    private static int LoadCount;

    private static void Setup(UnityModManager.ModEntry modEntry) {
        _task = Task.Run(async () => {
            try {
                domain ??= AppDomain.CurrentDomain;
                LoadJAModBootstrap(modEntry.Path);
                Task<bool> checkMod = Installer.CheckMod(modEntry);
                bool beta = InitializeVersion(modEntry);
                JAModInfo modInfo = LoadModInfo(modEntry, beta);
                if(await checkMod) {
                    beta = InitializeVersion(modEntry);
                    modInfo = LoadModInfo(modEntry, beta);
                }
                SetupJALib(modInfo);
            } catch (Exception e) {
                modEntry.Logger.LogException(e);
                throw;
            }
        });
    }

    private static void LoadJAModBootstrap(string path) {
        foreach(Assembly assembly in domain.GetAssemblies()) 
            if(assembly.GetName().Name == "JAMod.Bootstrap") return;
        Assembly.LoadFrom(Path.Combine(path, "JAMod.Bootstrap.dll"));
    }

    private static void SetupJALib(JAModInfo modInfo) {
        jalibModInfo = modInfo;
        LoadJAMod = (Action<JAModInfo>) Delegate.CreateDelegate(typeof(Action<JAModInfo>), LoadMod(modInfo).GetMethod("LoadModInfo", BindingFlags.NonPublic | BindingFlags.Static));
    }

    private static JAModInfo LoadModInfo(UnityModManager.ModEntry modEntry, bool beta) {
        string modInfoPath = Path.Combine(modEntry.Path, "JAModInfo.json");
        if(!File.Exists(modInfoPath)) throw new FileNotFoundException("JAModInfo not found.");
        JAModInfo modInfo = File.ReadAllText(modInfoPath).FromJson<JAModInfo>();
        if(modInfo.BootstrapVersion > BootstrapVersion) throw new Exception("Bootstrap version is too low.");
        modInfo.ModEntry = modEntry;
        modInfo.IsBetaBranch = beta;
        modEntry.Logger.Log("Successfully loaded JAModInfo");
        return modInfo;
    }

    private static bool InitializeVersion(UnityModManager.ModEntry modEntry) {
        try {
            string version = modEntry.Info.Version;
            string onlyVersion = version;
            string behindVersion = "";
            bool beta = version.Contains('-') || version.Contains(' ');
            if(beta) {
                int index = version.IndexOf('-');
                if(index == -1) index = version.IndexOf(' ');
                onlyVersion = version[..index];
                behindVersion = version[index..];
            }
            Version versionValue = Version.Parse(onlyVersion);
            modEntry.Info.Version = (versionValue.Build == 0     ? new Version(versionValue.Major, versionValue.Minor) :
                                     versionValue.Revision == -1 ? versionValue : new Version(versionValue.Major, versionValue.Minor, versionValue.Build)) + behindVersion;
            typeof(UnityModManager.ModEntry).GetField("Version", BindingFlags.Public | BindingFlags.Instance).SetValue(modEntry, versionValue);
            modEntry.Logger.Log("Version initialized to " + modEntry.Info.Version);
            return beta;
        } catch (Exception e) {
            modEntry.Logger.LogException(e);
            return false;
        }
    }

    private static Type LoadMod(JAModInfo modInfo) {
        if(modInfo.DependencyPath != null) {
            string dependencyPath = modInfo.DependencyRequireModPath ? Path.Combine(modInfo.ModEntry.Path, modInfo.DependencyPath) : modInfo.DependencyPath;
            foreach(string file in Directory.GetFiles(dependencyPath)) {
                try {
                    Assembly.LoadFrom(file);
                } catch (Exception e) {
                    modInfo.ModEntry.Logger.LogException(e);
                }
            }
        }
        Assembly modAssembly = Assembly.LoadFrom(modInfo.AssemblyRequireModPath ? Path.Combine(modInfo.ModEntry.Path, modInfo.AssemblyPath) : modInfo.AssemblyPath);
        Type modType = modAssembly.GetType(modInfo.ClassName);
        if(modType == null) throw new TypeLoadException("Type not found.");
        Activator.CreateInstance(modType, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, [modInfo.ModEntry], null, null);
        return modType;
    }

    public static void Load(UnityModManager.ModEntry modEntry) {
        LoadCount++;
        modEntry.Logger.Log("JABootstrap Load called. Count: " + LoadCount);
        modEntry.Info.DisplayName = modEntry.Info.Id + " <color=gray>[Waiting JALib...]</color>";
        Task.Run(async () => {
            try {
                bool beta = InitializeVersion(modEntry);
                JAModInfo modInfo = LoadModInfo(modEntry, beta);
                modEntry.Logger.Log("Now waiting for JALib to load...");
                try {
                    await _task;
                } catch (Exception) {
                    modEntry.Info.DisplayName = modEntry.Info.Id + " <color=red>[Error Loading JALib]</color>";
                    return;
                }
                modEntry.Logger.Log("JALib loaded. Now loading JAMod...");
                LoadJAMod(modInfo);
            } catch (Exception e) {
                modEntry.Logger.LogException(e);
                modEntry.Info.DisplayName = modEntry.Info.Id + " <color=red>[Error Loading JALib]</color>";
            }
        });
    }
}
