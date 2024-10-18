using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
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
    private static TcpClient client;
    private static JALibStatus jaLibStatus = JALibStatus.NotSet;
    private static Dictionary<string, string> dependencies;

    public static async Task Main(string[] args) {
        bool jalibInstall = false;
        Localization localization = Localization.Current;
        if(args.Length == 0) {
#if DEBUG
            args = [ "JALib", "1.0.0.0" ];
#else
            MessageBox.Show(localization.Error_ArgumentNotSet, localization.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
#endif
        }
        if(args.Length < 1) {
            MessageBox.Show(localization.Error_VersionNotSet, localization.Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
        new NotifyIcon {
            Icon = SystemIcons.Application,
            Visible = true,
            BalloonTipTitle = "모드 적용 안내",
            BalloonTipText = args[0] + "모드 적용을 시작합니다."
        }.ShowBalloonTip(5000);
        Task settingLoadTask = Task.Run(LoadSettings);
        Task modTask = ApplyMod(args[0], args[1], true);
        if(args[0] == "JALib") jalibInstall = true;
        await Task.WhenAll(settingLoadTask, modTask);
        string path = Path.Combine(adofaiPath, "Mods");
        List<Task> modInstallTasks = [];
        while(dependencies.Count > 0) {
            string modName = dependencies.Keys.First();
            Version version = new(dependencies[modName]);
            dependencies.Remove(modName);
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
        if(adofaiStatus == AdofaiStatus.Enabled) {
            using NetworkStream stream = client.GetStream();
            using BinaryWriter writer = new(stream);
            writer.Write(0);
            byte[] data = Encoding.UTF8.GetBytes(args[0]);
            writer.Write(data.Length);
            writer.Write(data);
            client.Close();
            goto End;
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
        DialogResult result = MessageBox.Show(adofaiStatus == AdofaiStatus.Enabled ? localization.AdofaiRestart : localization.AdofaiStart, localization.AdofaiRestartTitle,
            MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if(result == DialogResult.Yes) {
            if(adofaiStatus == AdofaiStatus.Enabled) {
                Process[] processes = Process.GetProcessesByName("A Dance of Fire and Ice");
                foreach(Process process in processes) process.Kill();
            }
            Process.Start(Path.Combine(adofaiPath, "A Dance of Fire and Ice.exe"));
        }
End:
        new NotifyIcon {
            Icon = SystemIcons.Application,
            Visible = true,
            BalloonTipTitle = "모드 적용 안내",
            BalloonTipText = args[0] + "모드 적용이 완료되었습니다."
        }.ShowBalloonTip(5000);
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
            }
            using Stream stream = await response.Content.ReadAsStreamAsync();
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, false, Encoding.UTF8);
            while(adofaiPath == null) await Task.Delay(10);
            string path = Path.Combine(adofaiPath, "Mods", modName);
            List<Task> tasks = [];
            if(!Directory.Exists(path)) Directory.CreateDirectory(path);
            foreach(ZipArchiveEntry entry in archive.Entries) {
                string entryPath = Path.Combine(path, entry.FullName);
                if(entryPath.EndsWith("/")) Directory.CreateDirectory(entryPath);
                else {
                    using FileStream fileStream = new(entryPath, FileMode.Create);
                    tasks.Add(entry.Open().CopyToAsync(fileStream));
                }
            }
            await Task.WhenAll(tasks);
            try {
                JObject modInfo = JObject.Parse(File.ReadAllText(Path.Combine(path, "JAModInfo.json")));
                if(core) dependencies = modInfo["Dependencies"].ToObject<Dictionary<string, string>>();
                else {
                    foreach(KeyValuePair<string, string> value in modInfo["Dependencies"].ToObject<Dictionary<string, string>>()) {
                        if(dependencies.ContainsKey(value.Key)) {
                            Version version1 = new(dependencies[value.Key]);
                            Version version2 = new(value.Value);
                            if(version1 < version2) dependencies[value.Key] = value.Value;
                        } else dependencies.Add(value.Key, value.Value);
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

    public static void LoadSettings() {
        try {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\JALib");
            adofaiPath = (string) key.GetValue("AdofaiPath");
            client = new TcpClient();
            int port = (int) key.GetValue("port");
            client.Connect("localhost", port);
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
                Environment.Exit(1);
            }
        }
    }
}