using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TinyJson;
using UnityModManagerNet;

namespace JALib.Bootstrap;

static class Installer {
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";

    internal static async Task<bool> CheckMod(UnityModManager.ModEntry modEntry) {
        string modName = modEntry.Info.Id;
        using HttpClient client = new();
        client.DefaultRequestHeaders.ExpectContinue = false;
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
            bool run;
            try {
                run = PatchCookieDomain();
            } catch (Exception) {
                run = CookieHandler.PatchCookieDomain();
            }
            if(run) return await CheckMod(modEntry);
            exception = e;
        } catch (Exception e) {
            exception = e;
        }
        modEntry.Logger.Error("Failed to connect to the auto installer server.");
        modEntry.Logger.LogException(exception);
        modEntry.Info.DisplayName = modName;
        return false;
    }

    private static bool PatchCookieDomain() => JAMod.Bootstrap.Installer.PatchCookieDomain();
}

static class CookieHandler {
    private static bool isPatched;

    public static bool PatchCookieDomain() {
        if(isPatched) return false;
        Harmony harmony = new("JAMod");
        harmony.Patch(typeof(CookieContainer).GetConstructor([]), transpiler: new HarmonyMethod(((Delegate) CookieDomainPatch).Method));
        isPatched = true;
        return true;
    }

    private static IEnumerable<CodeInstruction> CookieDomainPatch(IEnumerable<CodeInstruction> instructions) {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        while(enumerator.MoveNext()) {
            CodeInstruction instruction = enumerator.Current;
            if(instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo { Name: "InternalGetIPGlobalProperties" }) {
                yield return new CodeInstruction(OpCodes.Ldstr, "JALib-Custom");
                enumerator.MoveNext();
                enumerator.MoveNext();
                instruction = enumerator.Current;
            }
            yield return instruction;
        }
    }
}