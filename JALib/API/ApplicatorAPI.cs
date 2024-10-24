using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JALib.Core;
using JALib.Tools;
using JALib.Tools.ByteTool;
using UnityModManagerNet;

namespace JALib.API;

class ApplicatorAPI(TcpClient client) {
    private static TcpListener listener;
    private static Listener machine;

    public static int Connect() {
Setup:
        int port = JARandom.Instance.Next(49152, 65535);
        try {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();
            machine = new Listener();
            JALib.Instance.Log($"Listening on port: {port}");
        } catch (SocketException) {
            goto Setup;
        }
        return port;
    }

    public static void Dispose() {
        machine?.Dispose();
        listener?.Stop();
        machine = null;
        listener = null;
    }

    private void run() {
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
        else _ = mod.ForceReloadMod();
    }

    private class Listener : IAsyncStateMachine {
        private AsyncVoidMethodBuilder builder = AsyncVoidMethodBuilder.Create();
        private Task<TcpClient> task;

        public Listener() {
            builder.Start(ref machine);
        }

        public void MoveNext() {
            try {
                task ??= listener.AcceptTcpClientAsync();
                if(!task.IsCompleted) return;
                TcpClient client = task.Result;
                task = null;
                _ = JATask.Run(JALib.Instance, new ApplicatorAPI(client).run);
            } catch (ThreadAbortException) {
                builder.SetResult();
            } catch (Exception e) {
                JALib.Instance.LogException(e);
            }
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) {
        }

        public void Dispose() {
            if(task == null) return;
            task.Dispose();
            builder.SetResult();
        }
    }
}