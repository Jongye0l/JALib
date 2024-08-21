using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Tools;
using UnityModManagerNet;

namespace JALib.API.Packets;

class DownloadMod : RequestAPI {

    private string ModName;
    private Version ModVersion;
    private string ModPath;

    public DownloadMod(string modName, Version modVersion, string modPath = null) {
        ModName = modName;
        ModVersion = modVersion;
        ModPath = modPath ?? System.IO.Path.Combine(UnityModManager.modsPath, modName);
    }

    public override void ReceiveData(Stream input) {
        throw new NotSupportedException();
    }

    public override async Task Run(HttpClient client, string url) {
        try {
            Stream stream = await client.GetStreamAsync(url + $"downloadMod/{ModName}/{ModVersion}");
            Zipper.Unzip(stream, ModPath);
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + url);
            JALib.Instance.LogException(e);
        }
    }
}