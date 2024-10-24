using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.JAException;
using JALib.Tools;

namespace JALib.API;

class JApi {
    public static JApi Instance {
        get {
            _instance ??= new JApi();
            return _instance;
        }
    }

    private static JApi _instance;
    private static readonly HttpClient HttpClient = new();
    private static ConcurrentQueue<SendQueue> _queue = new();
    //private const string Domain1 = "jalibtest.jongyeol.kr";
    //private const string Domain2 = "jalibtest.jongyeol.kr";
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";
    private string domain;
    private TaskCompletionSource<bool> completeLoadTask = new();

    public static void Initialize() {
        HttpClient.DefaultRequestHeaders.ExpectContinue = false;
        _instance ??= new JApi();
    }

    public static Task<bool> CompleteLoadTask() {
        _instance ??= new JApi();
        return _instance.completeLoadTask?.Task ?? Task.FromResult(true);
    }

    private JApi() {
        TryConnect();
    }

    private void TryConnect() {
        JATask.Run(JALib.Instance, Connect);
    }

    private async void Connect() {
        try {
            if((await HttpClient.GetAsync($"https://{Domain1}/ping")).IsSuccessStatusCode) domain = Domain1;
            else if((await HttpClient.GetAsync($"https://{Domain2}/ping")).IsSuccessStatusCode) domain = Domain2;
            else throw new Exception("Failed to connect to the server");
            OnConnect();
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + domain);
            JALib.Instance.LogException(e);
            _instance.completeLoadTask.TrySetResult(false);
            Dispose();
        }
    }

    internal static async Task<T> Send<T>(T packet, bool wait) where T : GetRequest {
        if(_instance != null) {
            try {
                if(!await CompleteLoadTask()) throw new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", new Exception("Failed to connect server"));
                HttpResponseMessage response = await HttpClient.GetAsync($"https://{_instance.domain}/{packet.UrlBehind}");
                if(response.IsSuccessStatusCode) {
                    try {
                        await Task.Run(() => packet.Run(response));
                    } catch (Exception e) {
                        throw new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", e);
                    }
                    return packet;
                }
                string errorLog = "Error: " + response.StatusCode + " " + response.ReasonPhrase + " " + packet.GetType().Name;
                if(!wait) throw new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", new Exception(errorLog));
                if(response.StatusCode is not (HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable or (HttpStatusCode) 522)) {
                    JALib.Instance.Log(errorLog);
                    return packet;
                }
            } catch (HttpRequestException e) {
                if(!wait) throw new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", e);
            }
            if(wait) JALib.Instance.Log("Failed to connect server");
            _instance.Dispose();
        }
        if(!wait) throw new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", new Exception("Failed to connect server"));
        TaskCompletionSource<bool> taskCompletionSource = new();
        _queue.Enqueue(new SendQueue(packet, taskCompletionSource));
        await taskCompletionSource.Task;
        return packet;
    }

    private void OnConnect() {
        completeLoadTask.TrySetResult(true);
        completeLoadTask = null;
        while(_queue.TryDequeue(out SendQueue sendQueue)) Task.Run(sendQueue.run);
    }

    internal void Dispose() {
        _instance = null;
        completeLoadTask = null;
        GC.SuppressFinalize(this);
    }

    private class SendQueue(GetRequest packet, TaskCompletionSource<bool> tcs) {
        public async void run() {
            try {
                await Send(packet, true);
                tcs.SetResult(true);
            } catch (Exception e) {
                JALib.Instance.LogException("Failed Running Request Job " + packet.GetType().Name, e);
                tcs.SetException(e);
            }
        }
    }
}