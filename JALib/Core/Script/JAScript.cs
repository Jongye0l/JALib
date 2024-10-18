using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using JALib.API;
using JALib.API.Packets;
using TinyJson;

namespace JALib.Core.Script;

public class JAScript {
    private static Dictionary<string, JAScript> scripts = new();
    public string name { get; private set; }
    public string version { get; private set; }
    private Assembly assembly;

    internal JAScript(JAScriptInfo info, Assembly assembly) {
        name = info.Name;
        version = info.Version;
        this.assembly = assembly;
        scripts[name] = this;
    }

    public static JAScript GetScript(string name) => scripts.GetValueOrDefault(name);
    public static ICollection<JAScript> GetScripts() => scripts.Values;
    public static JAScript GetOrInstallScript(string name) => GetScript(name) ?? InstallScript(name).Result;
    public static async Task<JAScript> InstallScript(string name) {
        await JApi.Send(new DownloadScript(name));
        return LoadScript(name);
    }

    private static JAScript LoadScript(string name) {
        string folder = Path.Combine(JALib.Instance.Path, "Scripts", name);
        if(!File.Exists(Path.Combine(folder, "Info.json"))) throw new FileNotFoundException("Info.json not found");
        JAScriptInfo scriptInfo = File.ReadAllText(Path.Combine(folder, "Info.json")).FromJson<JAScriptInfo>();
        Assembly assembly = Assembly.LoadFrom(Path.Combine(folder, scriptInfo.AssemblyPath));
        return new JAScript(scriptInfo, assembly);
    }
}