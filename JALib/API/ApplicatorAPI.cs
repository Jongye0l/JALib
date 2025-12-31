using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;
using JALib.Tools.ByteTool;
using UnityModManagerNet;

namespace JALib.API;

class ApplicatorAPI(TcpClient client) {
    private static TcpListener listener;

    public static int Connect() {
Setup:
        int port = JARandom.Instance.Next(49152, 65535);
        try {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();
            Listen();
            JALib.Instance.Log($"Listening on port: {port}");
        } catch (SocketException) {
            goto Setup;
        } catch (Exception e) {
            JALib.Instance.LogException(e);
            throw;
        }
        return port;
    }

    public static void Dispose() {
        listener.Stop();
        listener = null;
    }

    public static void Listen() {
        listener.AcceptTcpClientAsync().OnCompleted(Work);
    }

    private static void Work(Task<TcpClient> task) {
        try {
            if(listener == null) return;
            TcpClient client = task.Result;
            Listen();
            JATask.Run(JALib.Instance, new ApplicatorAPI(client).Run);
        } catch (Exception e) {
            JALib.Instance.LogReportException("Fail To Generate TCP Server", e);
        }
    }

    private void Run() {
        using(client) {
            NetworkStream stream = client.GetStream();
            byte action = stream.ReadByteSafe();
            if(action == 0) {
                string modName = stream.ReadUTF();
                JALib.Instance.Log($"Applying mod: {modName}");
                LoadMod(modName);
            } else throw new Exception("Invalid action");
        }
    }

    public static void LoadMod(string modName) {
        JAMod mod = JAMod.GetMods(modName);
        if(mod == null) ForceApplyMod.ApplyMod(Path.Combine(UnityModManager.modsPath, modName));
        else mod.ForceReloadMod();
    }
}