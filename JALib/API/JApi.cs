using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JALib.API.Packets;
using JALib.JAException;
using JALib.Tools;
using UnityEngine;
using Ping = JALib.API.Packets.Ping;

namespace JALib.API;

internal static class JApi {
    private const string Domain = "jongyeol.kr";
    private const int Port1 = 43910;
    private const int Port2 = 43911;
    private const string Service = "jalib";
    private static JATcpClient _client;
    public static bool Connected => _client is { Connected: true };
    private static bool _adofaiEnable;
    private static bool _connectInfo;
    private static bool _statusExist;
    private static bool _connectFailed;
    private static Dictionary<long, RequestPacket> _requests = new();
    private static Discord.Discord discord;
    private static ConcurrentQueue<RequestPacket> _queue = new();
    
    internal static void Initialize() {
        _requests ??= new Dictionary<long, RequestPacket>();
        _queue ??= new ConcurrentQueue<RequestPacket>();
        TryConnect();
    }

    private static void TryConnect() {
        _connectFailed = false;
        JATask.Run(JALib.Instance, () => Connect(Port1));
        JATask.Run(JALib.Instance, () => Connect(Port2));
    }

    private static void Connect(int port) {
        JATcpClient client = new(new JAction(JALib.Instance, Read), false);
        client.SetConnectAction(new JAction(JALib.Instance, () => {
            JALib.Instance.Log("Successfully connected to the server port " + port);
            if(_client != null) {
                client.Dispose();
                return;
            }
            _client = client;
            OnConnect();
        }));
        client.SetCloseAction(new JAction(JALib.Instance, () => {
            if(_client != client) return;
            _client = null;
            OnClose();
        }));
        try {
            client.Connect(Domain, port, Service, true);
        } catch (Exception e) {
            JALib.Instance.Error("Failed to connect to the server port " + port);
            JALib.Instance.LogException(e);
            if(!_connectFailed) {
                _connectFailed = true;
                return;
            }
            Thread.Sleep(60000);
            TryConnect();
        }
    }

    private static void Read() {
        byte[] data;
        if(_client.ReadBoolean()) {
            long id = _client.ReadLong();
            data = _client.ReadBytesAndCount();
            if(!_requests.TryGetValue(id, out RequestPacket requestPacket)) return;
            requestPacket.ReceiveData(data);
            if(requestPacket is AsyncRequestPacket asyncPacket) asyncPacket.CompleteResponse();
            _requests.Remove(id);
            return;
        }
        string packetName = _client.ReadUTF();
        Type type = Type.GetType("JALib.API.Packets." + packetName);
        data = _client.ReadBytesAndCount();
        if(type == null) throw new NullReferenceException();
        type.New<ResponsePacket>().ReceiveData(data);
    }

    internal static void ResponseError(long id, string message) {
        if(!_requests.TryGetValue(id, out RequestPacket requestPacket)) return;
        Exception exception = new ResponseException(requestPacket.GetType().Name, message);
        JALib.Instance.LogException(exception);
        if(requestPacket is AsyncRequestPacket asyncPacket) asyncPacket.CompleteResponse();
        _requests.Remove(id);
    }
    
    internal static void Send(RequestPacket packet) {
        if(!Connected) {
            _queue.Enqueue(packet);
            return;
        }
        if(Thread.CurrentThread == MainThread.Thread) {
            JATask.Run(JALib.Instance, () => Send(packet));
            return;
        }
        lock(_client) {
            do {
                packet.ID = JARandom.Instance.NextLong();
            } while(_requests.ContainsKey(packet.ID));
            byte[] data = packet.GetBinary();
            _requests.Add(packet.ID, packet);
            _client.WriteUTF(packet.GetType().Name);
            _client.WriteLong(packet.ID);
            _client.WriteBytesAndCount(data);
        }
    }

    internal async static Task<T> SendAsync<T> (T packet) where T : AsyncRequestPacket {
        await JATask.Run(JALib.Instance, () => Send(packet));
        await packet.WaitResponse();
        return packet;
    }

    private static void OnConnect() {
        ConnectInfo();
        if(!_statusExist) JATask.Run(JALib.Instance, Status);
        while(Connected && _queue.TryDequeue(out RequestPacket packet)) Send(packet);
    }
    
    private async static void Status() {
        _statusExist = true;
        while(Connected) {
            int ping = (await SendAsync(new Ping())).ping;
            Send(new Status(ping, _requests.Keys.ToArray()));
            await Task.Delay(60000);
        }
        _statusExist = false;
    }
    
    private static void ConnectInfo() {
        if(_connectInfo || !_adofaiEnable) return;
        _connectInfo = true;
        Send(new ConnectInfo());
        discord = DiscordController.instance.GetValue<Discord.Discord>(nameof(discord));
        if(discord != null) discord.GetUserManager().OnCurrentUserUpdate += OnUserUpdate;
    }

    private static void OnClose() {
        foreach(RequestPacket value in _requests.Values) if(value is AsyncRequestPacket asyncPacket) asyncPacket.CompleteResponse();
        _requests.Clear();
        _connectInfo = false;
        if(discord != null) discord.GetUserManager().OnCurrentUserUpdate -= OnUserUpdate;
        JALib.Instance.Log("Disconnected from the server, trying to reconnect...");
        TryConnect();
    }

    private static void OnUserUpdate() {
        Send(new DiscordUpdate(discord.GetUserManager().GetCurrentUser().Id));
    }
    
    internal static void OnAdofaiStart() {
        _adofaiEnable = true;
        if(!_connectInfo && Connected) ConnectInfo();
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
            if(_connectInfo) OnUserUpdate();
            break;
        }
    }

    internal static void Dispose() {
        _client.Dispose();
        _client = null;
        _requests.Clear();
        _requests = null;
        _queue.Clear();
        _queue = null;
        discord = null;
    }
}