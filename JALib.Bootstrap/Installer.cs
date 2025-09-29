using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Bootstrap;

static class Installer {
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";

    internal static async Task<bool> CheckMod(UnityModManager.ModEntry modEntry) {
        string modName = modEntry.Info.Id;
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.ExpectContinue = false;
        client.DefaultRequestHeaders.Add("User-Agent", $"JALib Bootstrap/{typeof(Installer).Assembly.GetName().Version} ({GetOSInfo()})");
        string domain = Domain1;
        Exception exception;
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
                if(entryPath.EndsWith("/")) {
                    if(!Directory.Exists(entryPath)) Directory.CreateDirectory(entryPath);
                } else JAMod.Bootstrap.Installer.CopyFile(entryPath, entry);
            }
            string path = Path.Combine(modEntry.Path, "Info.json");
            if(!File.Exists(path)) path = Path.Combine(modEntry.Path, "info.json");
            UnityModManager.ModInfo modInfo = (await File.ReadAllTextAsync(path)).FromJson<UnityModManager.ModInfo>();
            typeof(UnityModManager.ModEntry).GetField("Info", BindingFlags.Public | BindingFlags.Instance).SetValue(modEntry, modInfo);
            return true;
        } catch (ArgumentException e) {
            if(JAMod.Bootstrap.Installer.PatchCookieDomain()) return await CheckMod(modEntry);
            exception = e;
        } catch (Exception e) {
            exception = e;
        }
        modEntry.Logger.Error("Failed to connect to the auto installer server.");
        modEntry.Logger.LogException(exception);
        modEntry.Info.DisplayName = modName;
        return false;
    }

    private static string GetOSInfo() {
        string os = SystemInfo.operatingSystem;
        Match m;
        string ver;
        Version version;

        if(os.Contains("Windows")) {
            m = Regex.Match(os, @"\(([\d\.]+)\) (\d+)bit");
            if(m.Success) {
                version = new Version(m.Groups[1].Value);
                ver = version.Major + "." + version.Minor;
            } else ver = "10.0";
            int bit = m.Success && int.TryParse(m.Groups[2].Value, out int b) ? b : 64;
            return $"Windows NT {ver}; " + (bit == 64 ? "Win64; x64" : "WOW64");
        }
        if(os.Contains("Linux")) {
            m = Regex.Match(os, @"Linux\s+([\d\.]+)");
            if(m.Success) {
                version = new Version(m.Groups[1].Value);
                ver = version.Major + "." + version.Minor;
            } else ver = "5.0";
            return $"X11; Linux {ver} x86_64";
        }
        if(os.Contains("Mac OS")) {
            m = Regex.Match(os, @"Mac OS X (\d+[\._]\d+[\._]?\d*)");
            ver = m.Success ? m.Groups[1].Value.Replace('_', '.') : "10.15.7";
            return $"Macintosh; Intel Mac OS X {ver}";
        }
        if(os.Contains("Android")) {
            m = Regex.Match(os, @"Android OS (\d+)");
            ver = m.Success ? m.Groups[1].Value : "10";
            return $"Linux; Android {ver}";
        }
        if(os.Contains("iOS")) {
            m = Regex.Match(os, @"iOS (\d+(\.\d+)*)");
            ver = m.Success ? m.Groups[1].Value : "16.0";
            return $"iPhone; CPU iPhone OS {ver.Replace('.', '_')} like Mac OS X";
        }
        return "Unknown";
    }
}
