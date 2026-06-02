using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib.API.Packets;

class DownloadMod : GetRequest {
    private string ModName;
    private Version ModVersion;
    private string ModPath;
    public Action<ProgressStream> OnProgressNeed;

    public DownloadMod(string modName, Version modVersion, string modPath = null) {
        ModName = modName;
        ModVersion = modVersion;
        ModPath = modPath ?? Path.Combine(UnityModManager.modsPath, modName);
    }

    public override string UrlBehind => $"downloadMod/{ModName}/{ModVersion}";

    public override async Task Run(HttpResponseMessage message) {
        long contentLength = message.Content.Headers.ContentLength ?? -1;
        Stream stream = await message.Content.ReadAsStreamAsync();
        try {
            if(contentLength == -1) {
                stream = new ProgressStream(stream, contentLength);
                OnProgressNeed?.Invoke(stream.AsUnsafe<ProgressStream>());
            }
            Zipper.Unzip(stream, ModPath);
        } finally {
            await stream.DisposeAsync();
        }
    }
}