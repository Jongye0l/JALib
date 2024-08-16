using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JALib.API.Packets;
using JALib.JAException;
using JALib.Stream;
using JALib.Tools;
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
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";
    private readonly HttpClient _httpClient = new();
    private JAWebSocketClient _client;
    private string domain;
    public static bool Connected => Instance?._client is { Connected: true };
    private static bool _adofaiEnable;
    private bool _connectInfo;
    private readonly Dictionary<long, RequestPacket> _requests = new();
    private static ConcurrentQueue<Request> _queue = new();
    private static Discord.Discord discord;

    public static void Initialize() {
        if(ADOBase.platform != Platform.None) OnAdofaiStart();
        _instance ??= new JApi();
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
            long currentTime = DateTimeOffset.UtcNow.Ticks;
            await _httpClient.GetAsync($"https://{domain}/ping");
            int ping = (int) (DateTimeOffset.UtcNow.Ticks - currentTime);
            if(pingTest.ping == -1) {
                pingTest.ping = ping;
                if(!pingTest.otherError) await Task.Delay(Math.Min(ping + 10, 300));
                if(pingTest.ping != ping) return;
            } else if(pingTest.ping > ping) pingTest.ping = ping;
            else return;
            this.domain = domain;
            _client = new JAWebSocketClient(new JAction(JALib.Instance, Read));
            await _client.ConnectAsync($"wss://{domain}/ws");
            OnConnect();
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + domain);
            JALib.Instance.LogException(e);
            if(pingTest.otherError) Dispose();
            else pingTest.otherError = true;
        }
    }

    private void Read() {
        using ByteArrayDataInput input = new(_client.ReadBytes(1024, false), JALib.Instance);
        if(input.ReadBoolean()) {
            long id = _client.ReadLong();
            if(!_requests.TryGetValue(id, out RequestPacket requestPacket)) return;
            requestPacket.ReceiveData(input);
            if(requestPacket is AsyncRequestPacket asyncPacket) asyncPacket.CompleteResponse();
            _requests.Remove(id);
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
            _queue.Enqueue(request);
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
                using ByteArrayDataOutput output = new(JALib.Instance);
                output.WriteUTF(packet.GetType().Name);
                output.WriteLong(packet.ID);
                packet.GetBinary(output);
                _instance._requests.Add(packet.ID, packet);
                _instance._client.WriteBytes(output.ToByteArray());
            }
        } else if(request is RequestAPI api) api.Run(_instance._httpClient, $"https://{_instance.domain}/");
    }

    internal async Task<T> SendAsync<T>(T packet) where T : AsyncRequestPacket {
        await JATask.Run(JALib.Instance, () => Send(packet));
        await packet.WaitResponse();
        return packet;
    }

    private void OnConnect() {
        ConnectInfo();
        while(_queue.TryDequeue(out Request request)) Send(request);
    }

    private void ConnectInfo() {
        if(_connectInfo || !_adofaiEnable) return;
        _connectInfo = true;
        Send(new ConnectInfo());
        discord = DiscordController.instance.GetValue<Discord.Discord>(nameof(discord));
        if(discord != null) discord.GetUserManager().OnCurrentUserUpdate += OnUserUpdate;
    }

    private static void OnUserUpdate() {
        if(Instance != null) Send(new DiscordUpdate(discord.GetUserManager().GetCurrentUser().Id));
    }

    internal static void OnAdofaiStart() {
        _adofaiEnable = true;
        if(Connected && !Instance._connectInfo) Instance.ConnectInfo();
        if(discord == null) MainThread.StartCoroutine(CheckDiscordCo());
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
            if(Instance._connectInfo) OnUserUpdate();
            break;
        }
    }

    internal void Dispose() {
        _client.Dispose();
        GC.SuppressFinalize(_requests);
        _instance = null;
        GC.SuppressFinalize(this);
    }

    private class PingTest {
        public int ping = -1;
        public bool otherError;
    }
}