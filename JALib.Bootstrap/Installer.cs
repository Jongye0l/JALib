using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Bootstrap;

static class Installer {
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";

    internal static async Task<bool> CheckMod(UnityModManager.ModEntry modEntry) {
        string modName = modEntry.Info.Id;
        using HttpClient client = new(new HttpClientHandler {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.ExpectContinue = false;
        client.DefaultRequestHeaders.Add("User-Agent", $"JALib Bootstrap/{typeof(Installer).Assembly.GetName().Version} ({GetOSInfo()})");
        string domain = Domain1;
        Exception exception;
        try {
            modEntry.Info.DisplayName = modName + "<color=grey> [Check Update...]</color>";
            HttpResponseMessage response = null;
            string version = modEntry.Info.Version.Split(" ")[0];
            for(int i = 0; i < 2; i++) {
                try {
                    response = await GetResponse(client, $"https://{domain}/autoInstaller/{version}");
                } catch (OperationCanceledException) {
                    UnityModManager.Logger.Log(domain + " request timed out. Retrying with another domain...");
                    domain = Domain2;
                    continue;
                }
                if(response.StatusCode == HttpStatusCode.NotModified) {
                    modEntry.Info.DisplayName = modName;
                    return false;
                }
                if(response.IsSuccessStatusCode) break;
                domain = Domain2;
            }
            response.EnsureSuccessStatusCode();
            modEntry.Info.DisplayName = modName + "<color=grey> [Updating...]</color>";
            long contentLength = response.Content.Headers.ContentLength ?? -1;
            await using Stream stream = new InstallStream(await response.Content.ReadAsStreamAsync(), contentLength, modEntry.Info);
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
            typeof(UnityModManager.ModEntry).GetField("Info", BindingFlags.Public | BindingFlags.Instance)!.SetValue(modEntry, modInfo);
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
    
    private static async Task<HttpResponseMessage> GetResponse(HttpClient client, string url) {
        Uri uri = new(url);
        Stopwatch stopwatch = new();
        while(true) {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            stopwatch.Restart();
            HttpResponseMessage response;
            try {
                response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            } catch (OperationCanceledException) {
                UnityModManager.Logger.Log($"Request timeout: {uri} ({stopwatch.ElapsedMilliseconds}ms)", "[JALib Bootstrap] ");
                throw;
            }
            UnityModManager.Logger.Log($"Request completed: {uri} ({stopwatch.ElapsedMilliseconds}ms)", "[JALib Bootstrap] ");
            if((uint) response.StatusCode < 300 || (uint) response.StatusCode >= 400 || (object) response.Headers.Location == null) return response;
            uri = response.Headers.Location;
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

    private class InstallStream(Stream baseStream, long length, UnityModManager.ModInfo modInfo) : Stream {
        private long _position;
        private int _lastPercent;

        private void CheckUpdate() {
            if(Length == -1) return;
            int percent = (int) (100 * _position / Length);
            if(percent != _lastPercent) {
                _lastPercent = percent;
                modInfo.DisplayName = modInfo.Id + "<color=grey> [Updating... 0%]</color>";
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
