using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Bootstrap;

class Installer {
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";

    internal static async Task<bool> CheckMod(UnityModManager.ModEntry modEntry) {
        string modName = modEntry.Info.Id;
        using HttpClient client = new();
        string domain = Domain1;
        try {
            modEntry.Info.DisplayName = modName + "<color=gray> [Check Update...]</color>";
            HttpResponseMessage response = null;
            string version = modEntry.Info.Version.Split(" ")[0];
            for(int i = 0; i < 2; i++) {
                response = await client.GetAsync($"https://{domain}/autoInstaller/{version}");
                if(response.StatusCode == HttpStatusCode.NotModified) {
                    modEntry.Info.DisplayName = modName;
                    return false;
                }
                if(response.IsSuccessStatusCode) break;
                domain = Domain2;
            }
            response.EnsureSuccessStatusCode();
            modEntry.Info.DisplayName = modName + "<color=gray> [Updating...]</color>";
            await using Stream stream = await response.Content.ReadAsStreamAsync();
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding.UTF8);
            foreach(ZipArchiveEntry entry in archive.Entries) {
                string entryPath = Path.Combine(modEntry.Path, entry.FullName);
                if(entryPath.EndsWith("/")) Directory.CreateDirectory(entryPath);
                else {
                    await using FileStream fileStream = new(entryPath, FileMode.Create);
                    await entry.Open().CopyToAsync(fileStream);
                }
            }
            string path = Path.Combine(modEntry.Path, "Info.json");
            if(!File.Exists(path)) path = Path.Combine(modEntry.Path, "info.json");
            UnityModManager.ModInfo modInfo = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
            typeof(UnityModManager.ModEntry).GetField("Info", AccessTools.all).SetValue(modEntry, modInfo);
            return true;
        } catch (Exception e) {
            modEntry.Logger.Error("Failed to connect to the auto installer server.");
            modEntry.Logger.LogException(e);
            modEntry.Info.DisplayName = modName;
            return false;
        }
    }
}