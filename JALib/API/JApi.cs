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
    private static ConcurrentQueue<(GetRequest, TaskCompletionSource<bool>)> _queue = new();
    //private const string Domain1 = "jalibtest.jongyeol.kr";
    //private const string Domain2 = "jalibtest.jongyeol.kr";
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";
    private string domain;
    private TaskCompletionSource<bool> completeLoadTask = new();

    public static void Initialize() {
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

    internal static async Task<T> Send<T>(T packet) where T : GetRequest {
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
                if(response.StatusCode is not (HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable or (HttpStatusCode) 522)) {
                    JALib.Instance.Log("Error: " + response.StatusCode + " " + response.ReasonPhrase + " " + packet.GetType().Name);
                    return packet;
                }
            } catch (HttpRequestException) {
            }
            JALib.Instance.Log("Failed to connect server");
            _instance.Dispose();
        }
        TaskCompletionSource<bool> taskCompletionSource = new();
        _queue.Enqueue((packet, taskCompletionSource));
        await taskCompletionSource.Task;
        return packet;
    }

    private void OnConnect() {
        completeLoadTask.TrySetResult(true);
        completeLoadTask = null;
        while(_queue.TryDequeue(out (GetRequest, TaskCompletionSource<bool>) result))
            Task.Run(async () => {
                try {
                    await Send(result.Item1);
                    result.Item2.SetResult(true);
                } catch (Exception e) {
                    JALib.Instance.LogException("Failed Running Request Job " + result.Item1.GetType().Name, e);
                    result.Item2.SetException(e);
                }
            });
    }

    internal void Dispose() {
        _instance = null;
        completeLoadTask = null;
        GC.SuppressFinalize(this);
    }
}