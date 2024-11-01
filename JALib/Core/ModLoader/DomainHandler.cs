using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JALib.Core.ModLoader;

class DomainHandler {
    private static AppDomain domain;
    private static List<AppDomain> domains = [];

    public static void Setup() {
        domain = AppDomain.CurrentDomain;
        domain.AssemblyResolve += DomainResolve;
        domains.Add(domain);
    }

    public static AppDomain CreateDomain(string name) {
        AppDomain newDomain = AppDomain.CreateDomain(name);
        newDomain.AssemblyResolve += DomainResolve;
        domains.Add(newDomain);
        return newDomain;
    }

    public static void UnloadDomain(AppDomain domain) {
        AppDomain.Unload(domain);
        domains.Remove(domain);
    }

    private static Assembly DomainResolve(object sender, ResolveEventArgs args) {
        foreach(Assembly assembly in domains.SelectMany(appDomain => appDomain.GetAssemblies())) if(assembly.FullName == args.Name) return assembly;
        return null;
    }
}