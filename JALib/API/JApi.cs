using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
        return _instance.completeLoadTask.Task;
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
        }
    }

    internal static async Task Send<T>(T packet) where T : GetRequest {
        if(_instance != null) {
            try {
                HttpResponseMessage response = await HttpClient.GetAsync($"https://{_instance.domain}/{packet.UrlBehind}");
                if(response.IsSuccessStatusCode) {
                    try {
                        await Task.Run(() => packet.Run(response));
                    } catch (Exception e) {
                        JALib.Instance.LogException("Failed Running Request Job " + packet.GetType().Name, e);
                    }
                    return;
                }
                if(response.StatusCode is not (HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable or (HttpStatusCode) 522)) {
                    JALib.Instance.Log("Error: " + response.StatusCode + " " + response.ReasonPhrase + " " + packet.GetType().Name);
                    return;
                }
            } catch (HttpRequestException) {
            }
            JALib.Instance.Log("Failed to connect server");
            _instance.Dispose();
        }
        TaskCompletionSource<bool> taskCompletionSource = new();
        _queue.Enqueue((packet, taskCompletionSource));
        await taskCompletionSource.Task;
    }

    private void OnConnect() {
        completeLoadTask.TrySetResult(true);
        completeLoadTask = null;
        while(_queue.TryDequeue(out (GetRequest, TaskCompletionSource<bool>) result))
            Send(result.Item1).ContinueWith(_ => result.Item2.SetResult(true));
    }

    internal void Dispose() {
        _instance = null;
        GC.SuppressFinalize(this);
    }
}