using System.IO;
using System.Threading.Tasks;
using JALib.API;
using JALib.API.Packets;
using JALib.Bootstrap;
using JALib.Tools;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Core.ModLoader;

class DownloadModData(JAModLoader data, Version targetVersion) {
    public Task<DownloadMod> downloadTask;
    private bool redownload;

    public void DownloadRequest(Version version) {
        if(targetVersion >= version) return;
        targetVersion = version;
        if(data.LoadState == ModLoadState.Downloading) redownload = true;
    }

    public void Download() {
        if(data.LoadState == ModLoadState.Downloading) return;
        data.LoadState = ModLoadState.Downloading;
        downloadTask = JApi.Send(new DownloadMod(data.name, targetVersion, data.RawModData?.info.ModEntry.Path), data.RawModData == null);
        downloadTask.GetAwaiter().UnsafeOnCompleted(DownloadComplete);
    }

    public void DownloadComplete() {
        string name = data.name;
        if(!downloadTask.IsCompletedSuccessfully) {
            (data.RawModData?.info.ModEntry.Logger ?? JALib.Instance.Logger).LogException($"Failed to download {name} mod", downloadTask.Exception);
            data.RawModData?.InstallFinish();
            return;
        }
        if(CheckReDownload()) return;
        if(data.RawModData == null) {
            UnityModManager.ModEntry modEntry = UnityModManager.modEntries.Find(entry => entry.Info.Id == name);
            string path = modEntry?.Path ?? Path.Combine(UnityModManager.modsPath, name);
            if(modEntry != null) {
                modEntry.Active = false;
                modEntry.OnUnload(modEntry);
                UnityModManager.modEntries.Remove(modEntry);
            }
            if(CheckReDownload()) return;
            data.LoadState = ModLoadState.None;
            ForceApplyMod.ApplyMod(path);
        } else {
            UnityModManager.ModEntry modEntry = data.RawModData.info.ModEntry;
            string path = Path.Combine(modEntry.Path, "Info.json");
            if(!File.Exists(path)) path = Path.Combine(modEntry.Path, "info.json");
            UnityModManager.ModInfo modInfo = File.ReadAllText(path).FromJson<UnityModManager.ModInfo>();
            modEntry.SetValue("Info", modInfo);
            bool beta = typeof(JABootstrap).Invoke<bool>("InitializeVersion", [modEntry]);
            data.RawModData.info = typeof(JABootstrap).Invoke<JAModInfo>("LoadModInfo", modEntry, beta);
            JAMod.SetupModInfo(data.RawModData.info);
            if(CheckReDownload()) return;
            data.RawModData.InstallFinish();
        }
        data.DownloadModData = null;
    }

    public bool CheckReDownload() {
        if(!redownload) return false;
        redownload = false;
        data.LoadState = ModLoadState.None;
        Download();
        return true;
    }
}