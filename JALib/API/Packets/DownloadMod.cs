using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib.API.Packets;

class DownloadMod : GetRequest {

    private string ModName;
    private Version ModVersion;
    private string ModPath;

    public DownloadMod(string modName, Version modVersion, string modPath = null) {
        ModName = modName;
        ModVersion = modVersion;
        ModPath = modPath ?? Path.Combine(UnityModManager.modsPath, modName);
    }

    public override string UrlBehind => $"downloadMod/{ModName}/{ModVersion}";

    public override async Task Run(HttpResponseMessage message) {
        await using Stream stream = await message.Content.ReadAsStreamAsync();
        Zipper.Unzip(stream, ModPath);
        JAMod mod = JAMod.GetMods(ModName);
        Task task = mod?.ForceReloadMod();
        if(task != null) await task;
    }
}