using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Bootstrap;

public class JABootstrap {
    public const int BootstrapVersion = 0;
    private static AppDomain domain;
    private static MethodInfo LoadJAMod;
    private static Task<bool> _task;
    internal static Harmony harmony;
    private static JAModInfo jalibModInfo;
    private static async void Setup(UnityModManager.ModEntry modEntry) {
        domain ??= AppDomain.CurrentDomain;
        _task = Installer.CheckMod(modEntry);
        bool beta = InitializeVersion(modEntry);
        JAModInfo modInfo = LoadModInfo(modEntry, beta);
        if(await _task) {
            beta = InitializeVersion(modEntry);
            modInfo = LoadModInfo(modEntry, beta);
        }
        SetupJALib(modInfo);
    }

    private static Type SetupJALib(JAModInfo modInfo) {
        jalibModInfo = modInfo;
        Type modType = LoadMod(modInfo);
        LoadJAMod = modType.GetMethod("LoadModInfo", (BindingFlags) 15420);
        return modType;
    }

    private static JAModInfo LoadModInfo(UnityModManager.ModEntry modEntry, bool beta) {
        string modInfoPath = Path.Combine(modEntry.Path, "JAModInfo.json");
        if(!File.Exists(modInfoPath)) throw new FileNotFoundException("JAModInfo not found.");
        JAModInfo modInfo = File.ReadAllText(modInfoPath).FromJson<JAModInfo>();
        if(modInfo.BootstrapVersion > BootstrapVersion) throw new Exception("Bootstrap version is too low.");
        modInfo.ModEntry = modEntry;
        modInfo.IsBetaBranch = beta;
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
            typeof(UnityModManager.ModEntry).GetField("Version", (BindingFlags) 15420).SetValue(modEntry, versionValue);
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
                    domain.Load(File.ReadAllBytes(file));
                } catch (Exception e) {
                    modInfo.ModEntry.Logger.LogException(e);
                }
            }
        }
        Assembly modAssembly = domain.Load(File.ReadAllBytes(modInfo.AssemblyRequireModPath ? Path.Combine(modInfo.ModEntry.Path, modInfo.AssemblyPath) : modInfo.AssemblyPath));
        Type modType = modAssembly.GetType(modInfo.ClassName);
        if(modType == null) throw new TypeLoadException("Type not found.");
        Activator.CreateInstance(modType, (BindingFlags) 15420, null, [modInfo.ModEntry], null, null);
        return modType;
    }

    public static async void Load(UnityModManager.ModEntry modEntry) {
        bool beta = InitializeVersion(modEntry);
        await _task;
        JAModInfo modInfo = LoadModInfo(modEntry, beta);
        LoadJAMod.Invoke(null, [modInfo]);
    }
}