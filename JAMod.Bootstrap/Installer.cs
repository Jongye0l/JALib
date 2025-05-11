using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

namespace JAMod.Bootstrap;

public static class Installer {
    public const string Domain1 = "jalib.jongyeol.kr";
    public const string Domain2 = "jalib2.jongyeol.kr";
    private static bool setupCookieDomain;

    internal static void InstallMod() {
        const string prefix = "[JAMod] ";
        const string exceptionPrefix = "[JAMod] [Exception] ";
        using HttpClient client = new();
        client.DefaultRequestHeaders.ExpectContinue = false;
        string domain = Domain1;
        Exception exception;
        try {
            foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix("<color=gray> [JALib Install : Check Server...]</color>");
            UnityModManager.Logger.Log("Checking server...", prefix);
            for(int i = 0; i < 2; i++) {
                Task<HttpResponseMessage> task = client.GetAsync($"https://{domain}/ping");
                task.Wait(10000);
                if(task.IsCompleted && task.Result.IsSuccessStatusCode) break;
                if(i == 1) task.Result.EnsureSuccessStatusCode();
                domain = Domain2;
            }
            foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix("<color=green> [JALib Installing...]</color>");
            UnityModManager.Logger.Log("Installing JALib...", prefix);
            using Stream stream = client.GetAsync($"https://{domain}/downloadMod/JALib/latest").Result.Content.ReadAsStreamAsync().Result;
            string path = Path.Combine(UnityModManager.modsPath, "JALib");
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding.UTF8);
            foreach(ZipArchiveEntry entry in archive.Entries) {
                string entryPath = Path.Combine(path, entry.FullName);
                if(entryPath.EndsWith("/")) {
                    if(!Directory.Exists(entryPath)) Directory.CreateDirectory(entryPath);
                } else CopyFile(entryPath, entry);
            }
            foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix("<color=green> [JALib Applying...]</color>");
            UnityModManager.Logger.Log("Applying JALib...", prefix);
            UnityModManager.ModEntry modEntry = ApplyMod(path);
            UnityModManager.Logger.Log("Apply Complete JALib", prefix);
            try {
                Action<UnityModManager.ModEntry> action = BootModData.CreateSetupAction(modEntry);
                foreach(BootModData modData in BootModData.bootModDataList) modData.Run(action);
            } catch (Exception e) {
                UnityModManager.Logger.LogException("Failed to run setup action", e, exceptionPrefix);
                foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix("<color=red> [JALib Setup Failed]</color>");
            }
            return;
        } catch (ArgumentException e) {
            if(PatchCookieDomain()) {
                InstallMod();
                return;
            }
            exception = e;
        } catch (Exception e) {
            exception = e;
        }
        UnityModManager.Logger.LogException("Failed to install JALib", exception, exceptionPrefix);
        foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix("<color=red> [JALib Install Failed]</color>");
    }
    
    private static UnityModManager.ModEntry ApplyMod(string path) {
        string path1 = Path.Combine(path, "Info.json");
        if(!File.Exists(path1)) path1 = Path.Combine(path, "info.json");
        if(!File.Exists(path1)) return null;
        UnityModManager.Logger.Log("Reading file '" + path1 + "'.");
        try {
            UnityModManager.ModInfo info = File.ReadAllText(path1).FromJson<UnityModManager.ModInfo>();
            if(string.IsNullOrEmpty(info.Id)) {
                UnityModManager.Logger.Error("Id is null.");
                return null;
            }
            if(string.IsNullOrEmpty(info.AssemblyName) && File.Exists(Path.Combine(path, info.Id + ".dll"))) info.AssemblyName = info.Id + ".dll";
            UnityModManager.ModEntry modEntry = new(info, path + Path.DirectorySeparatorChar);
            UnityModManager.modEntries.Add(modEntry);
            foreach(UnityModManager.Param.Mod mod in 
                    ((UnityModManager.Param) typeof(UnityModManager).GetMethod("get_Params", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null)).ModParams) 
                if(mod.Id == info.Id) modEntry.Enabled = mod.Enabled;
            if(modEntry.Enabled) modEntry.Active = true;
            return modEntry;
        } catch (Exception ex) {
            UnityModManager.Logger.Error("Error parsing file '" + path1 + "'.");
            Debug.LogException(ex);
            return null;
        }
    }
    
    public static bool PatchCookieDomain() {
        if(setupCookieDomain) return false;
        Harmony harmony = new("JAMod");
        harmony.Patch(typeof(CookieContainer).GetConstructor([]), transpiler: new HarmonyMethod(((Delegate) CookieDomainPatch).Method));
        setupCookieDomain = true;
        return true;
    }

    public static void CopyFile(string entryPath, ZipArchiveEntry entry) {
        string directory = Path.GetDirectoryName(entryPath);
        if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        FileStream fileStream = null;
        try {
            try {
                fileStream = File.Exists(entryPath) ? new FileStream(entryPath, FileMode.Truncate, FileAccess.Write, FileShare.None) : new FileStream(entryPath, FileMode.Create);
            } catch (IOException) {
                fileStream = new FileStream(entryPath, FileMode.Open, FileAccess.Write, FileShare.None);
            }
            entry.Open().CopyTo(fileStream);
            int left = (int) (fileStream.Length - fileStream.Position);
            if(left <= 0) return;
            byte[] buffer = new byte[left];
            for(int i = 0; i < left; i++) buffer[i] = 32;
            fileStream.Write(buffer, 0, left);
        } finally {
            fileStream?.Close();
        }
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