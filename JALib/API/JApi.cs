using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JALib.API.Packets;
using JALib.JAException;
using JALib.Tools;
using JALib.Tools.ByteTool;
using UnityEngine;

namespace JALib.API;

class JApi {
    public static JApi Instance {
        get {
            _instance ??= new JApi();
            return _instance;
        }
    }

    private static JApi _instance;
    //private const string Domain1 = "jalibtest.jongyeol.kr";
    //private const string Domain2 = "jalibtest.jongyeol.kr";
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";
    private readonly HttpClient _httpClient = new();
    private JAWebSocketClient _client;
    private string domain;
    public static bool Connected => Instance?._client is { Connected: true };
    internal static Task<bool> ConnectInfoTask;
    private readonly Dictionary<long, RequestPacket> _requests = new();
    private static ConcurrentQueue<(Request, TaskCompletionSource<bool>)> _queue = new();
    private static Discord.Discord discord;
    private TaskCompletionSource<bool> completeLoadTask = new();
    private static int reconnectAttemps;

    public static void Initialize() {
        _instance ??= new JApi();
    }

    public static Task<bool> CompleteLoadTask() {
        if(Connected) return Task.FromResult(true);
        _instance ??= new JApi();
        return _instance.completeLoadTask.Task;
    }

    private JApi() {
        TryConnect();
    }

    private void TryConnect() {
        PingTest pingTest = new();
        JATask.Run(JALib.Instance, () => Connect(Domain1, pingTest));
        JATask.Run(JALib.Instance, () => Connect(Domain2, pingTest));
    }

    private async void Connect(string domain, PingTest pingTest) {
        try {
            Stopwatch stopwatch = new();
            Task<HttpResponseMessage> task = _httpClient.GetAsync($"https://{domain}/ping");
            stopwatch.Start();
            try {
                (await task).EnsureSuccessStatusCode();
            } finally {
                stopwatch.Stop();
            }
            int ping = (int) stopwatch.ElapsedMilliseconds;
            JALib.Instance.Log("Ping to the server: " + domain + " " + ping + "ms");
            if(pingTest.ping == -1) {
                pingTest.ping = ping;
                if(!pingTest.otherError) await Task.Delay(Math.Min(ping + 10, 300));
                if(pingTest.ping != ping) return;
            } else if(pingTest.ping > ping) pingTest.ping = ping;
            else return;
            this.domain = domain;
            _client = new JAWebSocketClient(new JAction(JALib.Instance, Read));
            _client.SetCloseAction(new JAction(JALib.Instance, Dispose));
            await _client.ConnectAsync($"wss://{domain}/ws");
            OnConnect();
            reconnectAttemps = 0;
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + domain);
            JALib.Instance.LogException(e);
            if(pingTest.otherError) {
                completeLoadTask.TrySetResult(false);
                reconnectAttemps++;
                Dispose();
            } else pingTest.otherError = true;
        }
    }

    private void Read() {
        using Stream input = _client.ReadStream();
        if(input.ReadBoolean()) {
            long id = input.ReadLong();
            if(!_requests.TryGetValue(id, out RequestPacket requestPacket)) return;
            try {
                requestPacket.ReceiveData(input);
                if(requestPacket is AsyncRequestPacket asyncPacket) asyncPacket.CompleteResponse();
            } catch (Exception e) {
                if(requestPacket is AsyncRequestPacket asyncPacket) asyncPacket.FailResponse(e);
                throw;
            } finally {
                _requests.Remove(id);
            }
            return;
        }
        string packetName = input.ReadUTF();
        Type type = Type.GetType("JALib.API.Packets." + packetName);
        if(type == null) throw new NullReferenceException();
        type.New<ResponsePacket>().ReceiveData(input);
    }

    internal void ResponseError(long id, string message) {
        if(!_requests.TryGetValue(id, out RequestPacket requestPacket)) return;
        Exception exception = new ResponseException(requestPacket.GetType().Name, message);
        JALib.Instance.LogException(exception);
        if(requestPacket is AsyncRequestPacket asyncPacket) asyncPacket.CompleteResponse();
        _requests.Remove(id);
    }

    internal static void Send(Request request) {
        if(!Connected) {
            _queue.Enqueue((request, null));
            if(request is AsyncRequestPacket asyncRequestPacket) asyncRequestPacket.FailResponse(new Exception("Not connected to the server"));
            return;
        }
        if(Thread.CurrentThread == MainThread.Thread) {
            JATask.Run(JALib.Instance, () => Send(request));
            return;
        }
        if(request is RequestPacket packet) {
            lock (_instance._client) {
                do {
                    packet.ID = JARandom.Instance.NextLong();
                } while(_instance._requests.ContainsKey(packet.ID));
                using MemoryStream output = new();
                output.WriteUTF(packet.GetType().Name);
                output.WriteLong(packet.ID);
                packet.GetBinary(output);
                _instance._requests.Add(packet.ID, packet);
                _instance._client.WriteBytes(output.ToArray());
            }
        } else if(request is RequestAPI api) api.Run(_instance._httpClient, $"https://{_instance.domain}/");
    }

    internal static Task Send<T>(T packet) where T : RequestAPI {
        if(Connected)
            return Task.Run(async () => {
                await packet.Run(_instance._httpClient, $"https://{_instance.domain}/");
            });
        TaskCompletionSource<bool> taskCompletionSource = new();
        _queue.Enqueue((packet, taskCompletionSource));
        return taskCompletionSource.Task;
    }

    internal async Task<T> SendAsync<T>(T packet) where T : AsyncRequestPacket {
        await Task.Run(() => Send(packet));
        await packet.WaitResponse();
        return packet;
    }

    private void OnConnect() {
        completeLoadTask.TrySetResult(true);
        completeLoadTask = null;
        ConnectInfoTask = ConnectInfo();
        while(_queue.TryDequeue(out (Request, TaskCompletionSource<bool>) result)) {
            if(result.Item1 is RequestAPI requestAPI) {
                TaskCompletionSource<bool> item2 = result.Item2;
                Task.Run(async () => {
                    await requestAPI.Run(_httpClient, $"https://{domain}/");
                    item2.TrySetResult(true);
                });
            } else Send(result.Item1);
        }
    }

    private async Task<bool> ConnectInfo() {
        try {
            while(ADOBase.platform == Platform.None) await Task.Yield();
            await Task.Yield();
            await SendAsync(new ConnectInfo());
            discord = DiscordController.instance.GetValue<Discord.Discord>(nameof(discord));
            if(discord != null) discord.GetUserManager().OnCurrentUserUpdate += OnUserUpdate;
            else MainThread.StartCoroutine(CheckDiscordCo());
            return true;
        } catch (Exception e) {
            JALib.Instance.LogException(e);
            Dispose();
            return false;
        }
    }

    private static void OnUserUpdate() {
        if(Instance != null) Send(new DiscordUpdate(discord.GetUserManager().GetCurrentUser().Id));
    }

    private static IEnumerator CheckDiscordCo() {
        DiscordController controller = DiscordController.instance;
        while(true) {
            yield return new WaitForSeconds(60);
            DiscordController.instance = null;
            controller.Invoke("OnEnable");
            discord = controller.GetValue<Discord.Discord>(nameof(discord));
            if(discord == null) continue;
            if(Connected) discord.GetUserManager().OnCurrentUserUpdate += OnUserUpdate;
            if(ConnectInfoTask.IsCompleted && ConnectInfoTask.Result) OnUserUpdate();
            break;
        }
    }

    internal void Dispose() {
        _client.Dispose();
        GC.SuppressFinalize(_requests);
        _instance = null;
        GC.SuppressFinalize(this);
        if(!JALib.Instance.Enabled) return;
        if(reconnectAttemps < 1) _instance ??= new JApi();
        else
            Task.Delay(60000 * reconnectAttemps).ContinueWith(_ => {
                if(!JALib.Instance.Enabled) return;
                _instance ??= new JApi();
            });
    }

    private class PingTest {
        public int ping = -1;
        public bool otherError;
    }
}