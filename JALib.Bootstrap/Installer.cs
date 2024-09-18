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

public class Installer {
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";

    public static async Task CheckMod(UnityModManager.ModEntry modEntry) {
        string modName = modEntry.Info.DisplayName;
        try {
            using HttpClient client = new();
            modEntry.Info.DisplayName = modName + "<color=gray> [Checking Update...]</color>";
            string domain = Domain1;
            try {
                (await client.GetAsync($"https://{domain}/ping")).EnsureSuccessStatusCode();
            } catch {
                domain = Domain2;
                (await client.GetAsync($"https://{domain}/ping")).EnsureSuccessStatusCode();
            }
            HttpResponseMessage response = await client.GetAsync($"https://{domain}/autoInstaller/{modEntry.Info.Version.Split(" ")[0]}");
            if(response.StatusCode == HttpStatusCode.NotModified) {
                modEntry.Info.DisplayName = modName;
                return;
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
        } catch (Exception e) {
            modEntry.Logger.Error("Failed to connect to the auto installer server.");
            modEntry.Logger.LogException(e);
            modEntry.Info.DisplayName = modName;
        }
    }
}