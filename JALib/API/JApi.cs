using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JALib.JAException;
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
    private static readonly HttpClient HttpClient = new();
    private static ConcurrentQueue<Action> _queue = new();
    //private const string Domain1 = "jalibtest.jongyeol.kr";
    //private const string Domain2 = "jalibtest.jongyeol.kr";
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";
    private string domain;
    private TaskCompletionSource<bool> completeLoadTask = new();
    private int retryCount;

    public static void Initialize() {
        HttpClient.Timeout = TimeSpan.FromSeconds(10);
        HttpClient.DefaultRequestHeaders.ExpectContinue = false;
        HttpClient.SetupUserAgent("JALib", typeof(JApi).Assembly.GetName().Version.ToString());
        _instance ??= new JApi();
    }

    public static Task<bool> CompleteLoadTask() {
        _instance ??= new JApi();
        return _instance.completeLoadTask?.Task ?? Task.FromResult(true);
    }

    private JApi() {
        JATask.Run(JALib.Instance, Connect);
    }

    private void Connect() {
        if(_instance != this) return;
        try {
            domain = domain switch {
                null => Domain1,
                Domain1 => Domain2,
                _ => throw new Exception("Failed to connect to the server")
            };
            HttpClient.GetAsync($"https://{domain}/ping").OnCompleted(Connect);
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed to connect to the server: " + domain, e);
            _instance.completeLoadTask.TrySetResult(false);
            Restart();
        }
    }

    private void Connect(Task<HttpResponseMessage> t) {
        try {
            if(t.Exception != null) throw t.Exception.InnerException ?? t.Exception;
            if(t.Result.IsSuccessStatusCode) {
                OnConnect();
                return;
            }
            Connect();
        } catch (HttpRequestException e) {
            JALib.Instance.LogException(e);
            Connect();
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed to connect to the server: " + domain, e);
            _instance.completeLoadTask.TrySetResult(false);
            Restart();
        }
    }

    internal static Task<T> Send<T>(T packet, bool wait) where T : GetRequest {
        if(_instance == null && !wait)
            return Task.FromException<T>(new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", new Exception("Failed to connect server")));
        return new SendHandler<T>(packet, wait).tcs.Task;
    }

    private class SendHandler<T> where T : GetRequest {
        private readonly T packet;
        private readonly bool wait;
        internal readonly TaskCompletionSource<T> tcs = new();
        private Task<bool> loadTask;
        private HttpResponseMessage response;

        internal SendHandler(T packet, bool wait) {
            this.packet = packet;
            this.wait = wait;
            Setup();
        }

        private void Setup() {
            try {
                if(_instance != null) {
                    loadTask = CompleteLoadTask();
                    if(loadTask.IsCompleted) CheckConnect();
                    else loadTask.GetAwaiter().OnCompleted(CheckConnect);
                } else Queue();
            } catch (Exception e) {
                Error(e);
            }
        }

        private void CheckConnect() {
            try {
                if(!loadTask.Result) {
                    if(wait) Queue();
                    else Error(new Exception("Failed to connect server"));
                    return;
                }
                HttpClient.GetAsync($"https://{_instance.domain}/{packet.UrlBehind}").OnCompleted(Response);
            } catch (HttpRequestException e) {
                if(!wait) Queue();
                else Error(e);
            } catch (Exception e) {
                Error(e);
            }
        }

        private void Response(Task<HttpResponseMessage> t) {
            try {
                if(t.Exception != null) throw t.Exception.InnerException ?? t.Exception;
                response = t.Result;
                if(response.IsSuccessStatusCode) {
                    Task.Run(Run).GetAwaiter().OnCompleted(Complete);
                    return;
                }
                string errorLog = "Error: " + response.StatusCode + " " + response.ReasonPhrase + " " + packet.GetType().Name;
                if(!wait) {
                    Error(new Exception(errorLog));
                    return;
                }
                if(response.StatusCode is not (HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable or (HttpStatusCode) 522)) {
                    JALib.Instance.Error(errorLog);
                    Queue();
                }
            } catch (Exception e) {
                Error(e);
            }
        }

        private void Run() {
            try {
                packet.Run(response);
            } catch (Exception e) {
                JALib.Instance.Log("Error: " + response.StatusCode + " " + response.ReasonPhrase + " " + packet.GetType().Name);
                Error(e);
            }
        }

        private void Queue() => _queue.Enqueue(Setup);
        private void Complete() => tcs.SetResult(packet);
        private void Error(Exception e) => tcs.SetException(new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", e));
    }

    private void OnConnect() {
        completeLoadTask.TrySetResult(true);
        retryCount = 0;
        while(_queue.TryDequeue(out Action handler)) Task.Run(handler);
    }

    internal void Restart() {
        domain = null;
        Task.Delay(10000 * ++retryCount).OnCompleted(Connect);
    }

    internal void Dispose() {
        _instance = null;
        completeLoadTask?.SetCanceled();
        completeLoadTask = null;
        GC.SuppressFinalize(this);
    }
}