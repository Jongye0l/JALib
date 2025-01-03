using System.Collections.Generic;
using System.Reflection;
using dnlib.DotNet;

namespace JALib.Core.ModLoader;

class AssemblyLoader {
    public static Dictionary<string, Assembly> LoadedAssemblies = new();

    static AssemblyLoader() {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) => LoadedAssemblies.GetValueOrDefault(args.Name);

    public static Assembly LoadAssembly(string path) {
        Assembly assembly = Assembly.LoadFrom(path);
        LoadedAssemblies[assembly.GetName().Name[..^6]] = assembly;
        return assembly;
    }

    public static void CreateCacheAssembly(string path, string cachePath) {
        ModuleDef module = ModuleDefMD.Load(path);
        SetupName(module);
        module.Write(cachePath);
    }

    private static void SetupName(ModuleDef module) {
        AssemblyDef assembly = module.Assembly;
        assembly.Name += "-JAMod";
    }

    public static void CreateCacheReloadAssembly(string path, string cachePath, int reloadCount) {
        ModuleDef module = ModuleDefMD.Load(path);
        SetupName(module);
        foreach(TypeDef type in module.GetTypes()) {
            if(type.IsInterface || type.IsAbstract) continue;
            if(CheckType(type.BaseType)) type.Name += reloadCount;
        }
        module.Write(cachePath);
    }

    private static bool CheckType(ITypeDefOrRef type) {
        while(true) {
            if(type == null) return false;
            if(type.FullName == "UnityEngine.MonoBehaviour") return true;
            type = type.GetBaseType();
        }
    }
}