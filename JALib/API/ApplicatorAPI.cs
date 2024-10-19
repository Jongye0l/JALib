using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;
using JALib.Tools.ByteTool;
using UnityModManagerNet;

namespace JALib.API;

class ApplicatorAPI {
    private static TcpListener listener;
    private static Task listenerTask;

    public static int Connect() {
        int port = JARandom.Instance.Next(49152, 65535);
        listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        listener.Start();
        listenerTask = Task.Run(Listen);
        JALib.Instance.Log($"Listening on port: {port}");
        return port;
    }

    public static void Dispose() {
        listener?.Stop();
        listenerTask?.Dispose();
        listener = null;
        listenerTask = null;
    }

    public static async void Listen() {
        while(true) {
            try {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = JATask.Run(JALib.Instance, () => {
                    using (client) {
                        NetworkStream stream = client.GetStream();
                        byte action = stream.ReadByteSafe();
                        if(action == 0) {
                            string modName = stream.ReadUTF();
                            JALib.Instance.Log($"Applying mod: {modName}");
                            LoadMod(modName);
                        } else throw new Exception("Invalid action");
                    }
                });
            } catch (ThreadAbortException) {
                break;
            } catch (Exception e) {
                JALib.Instance.LogException(e);
            }
        }
    }

    public static void LoadMod(string modName) {
        JAMod mod = JAMod.GetMods(modName);
        if(mod == null) ForceApplyMod.ApplyMod(Path.Combine(UnityModManager.modsPath, modName));
        else _ = mod.ForceReloadMod();
    }
}