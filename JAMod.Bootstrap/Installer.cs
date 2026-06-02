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
using System.Threading;
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

    internal static async Task InstallMod() {
        try {
            const string prefix = "[JAMod] ";
            const string exceptionPrefix = "[JAMod] [Exception] ";
            using HttpClient client = new();
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Add("User-Agent", $"JAMod Bootstrap/{typeof(Installer).Assembly.GetName().Version} ({GetOSInfo()})");
            string domain = Domain1;
            Exception exception;
            try {
                foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix("<color=grey> [JALib Install : Check Server...]</color>");
                UnityModManager.Logger.Log("Checking server...", prefix);
                for(int i = 0; i < 2; i++) {
                    try {
                        using CancellationTokenSource cts1 = new(TimeSpan.FromSeconds(5));
                        HttpResponseMessage response = await client.GetAsync($"https://{domain}/ping", HttpCompletionOption.ResponseHeadersRead, cts1.Token);
                        if(response.IsSuccessStatusCode) break;
                        if(i == 1) response.EnsureSuccessStatusCode();
                    } catch (TaskCanceledException) {
                        // ignored
                    }
                    domain = Domain2;
                }
                foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix("<color=green> [JALib Installing...]</color>");
                UnityModManager.Logger.Log("Installing JALib...", prefix);
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
                HttpResponseMessage message = await client.GetAsync($"https://{domain}/downloadMod/JALib/latest", HttpCompletionOption.ResponseHeadersRead, cts.Token);
                long contentLength = message.Content.Headers.ContentLength ?? -1;
                await using Stream stream = new InstallStream(await message.Content.ReadAsStreamAsync(), contentLength);
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
        } catch (Exception e) {
            UnityModManager.Logger.LogException("Unexpected error during installation", e, "[JAMod] [Exception] ");
        }
    }

    private static string GetOSInfo() {
#if TEST
        return "Windows NT 10.0; Win64; x64";
#else
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
#endif
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
                if(mod.Id == info.Id)
                    modEntry.Enabled = mod.Enabled;
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
        if(directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
        FileStream fileStream = null;
        try {
            try {
                fileStream = File.Exists(entryPath) ? new FileStream(entryPath, FileMode.Truncate, FileAccess.Write, FileShare.None) : new FileStream(entryPath, FileMode.Create);
            } catch (IOException) {
                fileStream = new FileStream(entryPath, FileMode.Open, FileAccess.Write, FileShare.None);
            }
            using (Stream st = entry.Open()) st.CopyTo(fileStream);
            int left = (int) (fileStream.Length - fileStream.Position);
            if(left > 0) fileStream.SetLength(fileStream.Position);
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

    private class InstallStream(Stream baseStream, long length) : Stream {
        private long _position;
        private int _lastPercent;

        private void CheckUpdate() {
            if(Length == -1) return;
            int percent = (int) (100 * _position / Length);
            if(percent != _lastPercent) {
                _lastPercent = percent;
                foreach(BootModData modData in BootModData.bootModDataList) modData.SetPostfix($"<color=grey> [JALib Installing... {percent}%]</color>");
            }
        }

        public override void Flush() => baseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        
        public override int Read(byte[] buffer, int offset, int count) {
            int read = baseStream.Read(buffer, offset, count);
            _position += read;
            CheckUpdate();
            return read;
        }

        public override int ReadByte() {
            int read = baseStream.ReadByte();
            if(read != -1) {
                _position++;
                CheckUpdate();
            }
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            int read = await baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            _position += read;
            CheckUpdate();
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get; } = length;

        public override long Position {
            get => _position;
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing) {
            if(!disposing) return;
            baseStream.Dispose();
        }
    }
}