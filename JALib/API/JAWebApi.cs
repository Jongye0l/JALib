using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib.API;

internal class JAWebApi {
    
    public const string BaseUrl = "https://jongyeol.kr/api/";
    public const string DownloadUrl = BaseUrl + "download/{0}/{1}";
    
    public async static void DownloadMod(string modName, Version modVersion = null, string modPath = null) {
        modPath ??= Path.Combine(UnityModManager.modsPath, modName);
        if(!Directory.Exists(modPath)) Directory.CreateDirectory(modPath);
        await InstallMod(modName, modVersion, modPath);
        ForceApplyMod.ApplyMod(modPath);
    }
    
    public async static void DownloadMod(JAMod mod, bool force) {
        await InstallMod(mod.Name, null, mod.Path);
        if(force) mod.ModEntry.Invoke("Reload");
        else mod.ModEntry.SetValue("CanReload", true);
    }
    
    private async static Task InstallMod(string modName, Version modVersion, string modPath) {
        using HttpClient client = new();
        await using System.IO.Stream stream = await client.GetStreamAsync(string.Format(DownloadUrl, modName, modVersion?.ToString() ?? "latest"));
        Zipper.Unzip(stream, modPath);
    }
}