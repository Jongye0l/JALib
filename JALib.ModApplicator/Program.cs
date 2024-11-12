using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JALib.ModApplicator.Resources;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace JALib.ModApplicator;

class Program {
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";
    private static string adofaiPath;
    private static AdofaiStatus adofaiStatus = AdofaiStatus.NotSet;
    private static int port;
    private static JALibStatus jaLibStatus = JALibStatus.NotSet;
    private static Dictionary<string, string> dependencies;

    public static async Task Main(string[] args) {
        bool jalibInstall = false;
        Localization localization = Localization.Current;
        if(args.Length == 0) {
#if DEBUG
            args = [ "jalib://autoApplicator/JALib/1.0.0.9" ];
#else
            MessageBox.Show(localization.Error_ArgumentNotSet, localization.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);
            return;
#endif
        }
        args = args[0].Replace("jalib://autoApplicator/", "").Split('/');
        if(args.Length < 2) {
            MessageBox.Show(localization.Error_VersionNotSet, localization.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);
            return;
        }
        Icon icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        NotifyIcon notifyIcon = new() {
            Icon = icon,
            Text = string.Format(localization.ModInstalling, args[0]),
            Visible = true
        };
        LoadSettings();
        string path = Path.Combine(adofaiPath, "Mods");
        string curModPath = Path.Combine(path, args[0]);
        if(Directory.Exists(curModPath)) {
            string infoPath = Path.Combine(curModPath, "Info.json");
            if(!File.Exists(infoPath)) infoPath = Path.Combine(curModPath, "info.json");
            if(File.Exists(infoPath)) {
                JObject modInfo = JObject.Parse(File.ReadAllText(infoPath));
                string versionString = modInfo["Version"].ToString();
                versionString = versionString.Split(' ')[0];
                versionString = versionString.Split('-')[0];
                Version version1 = new(versionString);
                if(version1 == new Version(args[1])) {
                    notifyIcon.ShowBalloonTip(3000, localization.ModAlreadyTitle, string.Format(localization.ModAlreadyInstalled, args[0]), ToolTipIcon.Warning);
                    return;
                }
            }
        }
        Task modTask = ApplyMod(args[0], args[1], true);
        if(args[0] == "JALib") jalibInstall = true;
        await modTask;
        notifyIcon.Text = string.Format(localization.DependenciesInstalling, args[0]);
        List<Task> modInstallTasks = [];
        Dictionary<string, Version> requestDependencies = new();
        while(dependencies.Count > 0) {
            string modName = dependencies.Keys.First();
            Version version = new(dependencies[modName]);
            dependencies.Remove(modName);
            if(requestDependencies.ContainsKey(modName)) {
                if(requestDependencies[modName] < version) requestDependencies[modName] = version;
                else continue;
            } else requestDependencies.Add(modName, version);
            string infoPath = Path.Combine(path, modName, "Info.json");
            if(!File.Exists(infoPath)) infoPath = Path.Combine(path, modName, "info.json");
            if(File.Exists(infoPath)) {
                try {
                    JObject modInfo = JObject.Parse(File.ReadAllText(infoPath));
                    string versionString = modInfo["Version"].ToString();
                    versionString = versionString.Split(' ')[0];
                    versionString = versionString.Split('-')[0];
                    Version version1 = new(versionString);
                    if(version1 >= version) continue;
                } catch (Exception) {
                    // ignored
                }
            }
            if(modName == "JALib") jalibInstall = true;
            modInstallTasks.Add(ApplyMod(modName, version.ToString(), false));
        }
        await Task.WhenAll(modInstallTasks);
        bool adofaiRestart = false;
        if(adofaiStatus == AdofaiStatus.EnabledWithMod) {
            try {
                using(TcpClient client = new()) {
                    await client.ConnectAsync("localhost", port);
                    using NetworkStream stream = client.GetStream();
                    stream.WriteByte(0);
                    byte[] data = Encoding.UTF8.GetBytes(args[0]);
                    stream.WriteByte((byte) (data.Length >> 24));
                    stream.WriteByte((byte) (data.Length >> 16));
                    stream.WriteByte((byte) (data.Length >> 8));
                    stream.WriteByte((byte) data.Length);
                    stream.Write(data, 0, data.Length);
                }
                goto End;
            } catch (Exception) {
                goto AdofaiRestart;
            }
        }
        switch(jaLibStatus) {
            case JALibStatus.NotInstalled: goto DownloadJALib;
            case JALibStatus.NotEnabled:
            case JALibStatus.UmmNotInstalled:
            case JALibStatus.UmmNotInitialized: goto End;
            case JALibStatus.Error:
            case JALibStatus.Enabled: goto AdofaiRestart;
        }
DownloadJALib:
        if(!jalibInstall) await ApplyMod("JALib", "latest", false);
AdofaiRestart:
        adofaiRestart = true;
End:
        notifyIcon.Text = string.Format(localization.ModApplyFinish, args[0]);
        notifyIcon.ShowBalloonTip(3000, localization.ModAnnounceTitle, args[0] + localization.FinishModApply, ToolTipIcon.Info);
        if(adofaiRestart) {
            DialogResult result = MessageBox.Show(adofaiStatus == AdofaiStatus.Enabled ? localization.AdofaiRestart : localization.AdofaiStart, localization.AdofaiRestartTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if(result == DialogResult.Yes) {
                if(adofaiStatus == AdofaiStatus.Enabled) {
                    Process[] processes = Process.GetProcessesByName("A Dance of Fire and Ice");
                    foreach(Process process in processes) process.Kill();
                }
                Process.Start(Path.Combine(adofaiPath, "A Dance of Fire and Ice.exe"));
            }
        }
    }

    public static async Task ApplyMod(string modName, string version, bool core) {
        try {
            HttpResponseMessage response = null;
            string domain = Domain1;
            using HttpClient client = new();
            for(int i = 0; i < 2; i++) {
                response = await client.GetAsync($"https://{domain}/downloadMod/{modName}/{version}");
                if(response.IsSuccessStatusCode) break;
                domain = Domain2;
            }
            if(!response.IsSuccessStatusCode) {
                MessageBox.Show(Localization.Current.Error_FailConnectServer, Localization.Current.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
                return;
            }
            using Stream stream = await response.Content.ReadAsStreamAsync();
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding.UTF8);
            while(adofaiPath == null) await Task.Delay(10);
            string path = Path.Combine(adofaiPath, "Mods", modName);
            if(!Directory.Exists(path)) Directory.CreateDirectory(path);
            foreach(ZipArchiveEntry entry in archive.Entries) {
                string entryPath = Path.Combine(path, entry.FullName);
                if(entryPath.EndsWith("/")) Directory.CreateDirectory(entryPath);
                else CopyFile(entryPath, entry);
            }
            try {
                JObject modInfo = JObject.Parse(File.ReadAllText(Path.Combine(path, "JAModInfo.json")));
                if(core) dependencies = modInfo["Dependencies"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();
                else {
                    if(modInfo.ContainsKey("Dependencies")) {
                        foreach(KeyValuePair<string, string> value in modInfo["Dependencies"].ToObject<Dictionary<string, string>>()) {
                            if(dependencies.ContainsKey(value.Key)) {
                                Version version1 = new(dependencies[value.Key]);
                                Version version2 = new(value.Value);
                                if(version1 < version2) dependencies[value.Key] = value.Value;
                            } else dependencies.Add(value.Key, value.Value);
                        }
                    }
                }
            } catch (Exception) {
                MessageBox.Show(Localization.Current.Error_LoadModInfo, Localization.Current.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                dependencies = new Dictionary<string, string>();
            }
        } catch (Exception e) {
            MessageBox.Show(e.ToString(), Localization.Current.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);
        }
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
            byte[] buffer = new byte[Math.Max(0, left)];
            for(int i = 0; i < buffer.Length; i++) buffer[i] = 32;
            fileStream.Write(buffer, 0, buffer.Length);
        } finally {
            fileStream?.Close();
        }
    }

    public static void LoadSettings() {
        try {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\JALib");
            adofaiPath = (string) key.GetValue("AdofaiPath");
            port = (int) key.GetValue("port");
            adofaiStatus = AdofaiStatus.EnabledWithMod;
        } catch {
            try {
                adofaiStatus = Process.GetProcessesByName("A Dance of Fire and Ice").Length == 0 ? AdofaiStatus.NotEnabled : AdofaiStatus.Enabled;
                string jalibPath = Path.Combine(adofaiPath, "Mods", "JALib", "JALib.dll");
                if(!File.Exists(jalibPath)) {
                    jaLibStatus = JALibStatus.NotInstalled;
                    return;
                }
                string settingPath = Path.Combine(adofaiPath, "A Dance of Fire and Ice_Data", "Managed", "UnityModManager");
                if(!Directory.Exists(settingPath)) {
                    jaLibStatus = JALibStatus.UmmNotInstalled;
                    return;
                }
                if(!File.Exists(Path.Combine(settingPath, "Params.xml"))) {
                    jaLibStatus = JALibStatus.UmmNotInitialized;
                    return;
                }
                string data = File.ReadAllText(Path.Combine(settingPath, "Params.xml")).ToLower();
                if(data.Contains("""
                                 <mod id="jalib" enabled="false"
                                 """)) jaLibStatus = JALibStatus.NotEnabled;
                else if(data.Contains("""
                                      <mod id="jalib" enabled="true"
                                      """)) jaLibStatus = JALibStatus.Enabled;
                else jaLibStatus = JALibStatus.Error;
            } catch (Exception e) {
                MessageBox.Show(Localization.Current.Error_LoadAdofaiPath + e, Localization.Current.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
        }
    }
}