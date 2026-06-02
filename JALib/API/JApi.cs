using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
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
    public static readonly HttpClient HttpClient = new(new HttpClientHandler {
        AllowAutoRedirect = false
    });
    private static readonly Queue<Action> Queue = new();
    //private const string Domain1 = "jalibtest.jongyeol.kr";
    //private const string Domain2 = "jalibtest.jongyeol.kr";
    private const string Domain1 = "jalib.jongyeol.kr";
    private const string Domain2 = "jalib2.jongyeol.kr";
    private const int HeaderTimeoutSeconds = 10;
    private string _domain;
    private TaskCompletionSource<bool> _completeLoadTask = new();
    private int _retryCount;

    public static void Initialize() {
        HttpClient.DefaultRequestHeaders.ExpectContinue = false;
        HttpClient.SetupUserAgent("JALib", typeof(JApi).Assembly.GetName().Version.ToString());
        _instance ??= new JApi();
    }

    public static Task<bool> CompleteLoadTask() {
        _instance ??= new JApi();
        return _instance._completeLoadTask?.Task ?? Task.FromResult(true);
    }

    private JApi() {
        JATask.Run(JALib.Instance, Connect);
    }

    private void Connect() {
        if(_instance != this) return;
        try {
            _domain = _domain switch {
                null => Domain1,
                Domain1 => Domain2,
                _ => throw new Exception("Failed to connect to the server")
            };
            GetResponse($"https://{_domain}/ping").OnCompleted(Connect);
        } catch (Exception e) {
            JALib.Instance.LogReportException("Failed to connect to the server: " + _domain, e);
            _instance._completeLoadTask.TrySetResult(false);
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
            JALib.Instance.LogReportException("Failed to connect to the server: " + _domain, e);
            _instance._completeLoadTask.TrySetResult(false);
            Restart();
        }
    }

    internal static Task<T> Send<T>(T packet, bool wait) where T : GetRequest {
        if(_instance == null && !wait)
            return Task.FromException<T>(new PacketRunningException($"Failed Running Request Job {packet.GetType().Name}(URL Behind: {packet.UrlBehind})", new Exception("Failed to connect server")));
        return new SendHandler<T>(packet, wait).Tcs.Task;
    }
    
    private static async Task<HttpResponseMessage> GetResponse(string url, bool wait = false) {
        Uri uri = new(url, UriKind.RelativeOrAbsolute);
        Stopwatch stopwatch = new();
        while(true) {
            using CancellationTokenSource cts = wait ? new CancellationTokenSource(TimeSpan.FromSeconds(HeaderTimeoutSeconds)) : null;
            stopwatch.Restart();
            HttpResponseMessage response;
            try {
                response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts?.Token ?? CancellationToken.None);
            } catch (OperationCanceledException) {
                if(JALib.Instance.Setting.LogApiRequests) JALib.Instance.Log($"Request timeout: {uri} ({stopwatch.ElapsedMilliseconds}ms)");
                throw;
            }
            if(JALib.Instance.Setting.LogApiRequests) JALib.Instance.Log($"Request completed: {uri} ({stopwatch.ElapsedMilliseconds}ms)");
            if((uint) response.StatusCode < 300 || (uint) response.StatusCode >= 400 || (object) response.Headers.Location == null) return response;
            uri = response.Headers.Location;
        }
    }

    private class SendHandler<T> where T : GetRequest {
        private readonly T _packet;
        private readonly bool _wait;
        internal readonly TaskCompletionSource<T> Tcs = new();
        private Task<bool> _loadTask;
        private HttpResponseMessage _response;

        internal SendHandler(T packet, bool wait) {
            _packet = packet;
            _wait = wait;
            Setup();
        }

        private void Setup() {
            try {
                if(_instance != null) {
                    _loadTask = CompleteLoadTask();
                    if(_loadTask.IsCompleted) CheckConnect();
                    else _loadTask.GetAwaiter().OnCompleted(CheckConnect);
                } else Enqueue();
            } catch (Exception e) {
                Error(e);
            }
        }

        private void CheckConnect() {
            try {
                if(!_loadTask.Result) {
                    if(_wait) Enqueue();
                    else Error(new Exception("Failed to connect server"));
                    return;
                }
                GetResponse($"https://{_instance._domain}/{_packet.UrlBehind}", true).OnCompleted(Response);
            } catch (HttpRequestException e) {
                if(!_wait) Enqueue();
                else Error(e);
            } catch (Exception e) {
                Error(e);
            }
        }

        private void Response(Task<HttpResponseMessage> t) {
            try {
                if(t.Exception != null) {
                    Error(t.Exception.InnerException ?? t.Exception);
                    return;
                }
                _response = t.Result;
                if((uint) _response.StatusCode >= 200 && (uint) _response.StatusCode < 400) {
                    Task.Run(Run).GetAwaiter().OnCompleted(Complete);
                    return;
                }
                string errorLog = "Error: " + _response.StatusCode + " " + _response.ReasonPhrase;
                if(!_wait) {
                    Error(new Exception(errorLog));
                    return;
                }
                if(_response.StatusCode is not (HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable or (HttpStatusCode) 522)) {
                    JALib.Instance.Error(errorLog);
                    Enqueue();
                }
            } catch (Exception e) {
                Error(e);
            }
        }

        private async Task Run() {
            try {
                await _packet.Run(_response);
            } catch (Exception e) {
                JALib.Instance.Log("Error: " + _response.StatusCode + " " + _response.ReasonPhrase + " " + _packet.GetType().Name);
                Error(e);
            }
        }

        private void Enqueue() => Queue.Enqueue(Setup);
        private void Complete() => Tcs.SetResult(_packet);
        private void Error(Exception e) => Tcs.SetException(new PacketRunningException($"Failed Running Request Job {_packet.GetType().Name}(URL Behind: {_packet.UrlBehind})", e));
    }

    private void OnConnect() {
        _completeLoadTask.TrySetResult(true);
        _retryCount = 0;
        while(Queue.TryDequeue(out Action handler)) Task.Run(handler);
    }

    internal void Restart() {
        _domain = null;
        Task.Delay(10000 * ++_retryCount).OnCompleted(Connect);
    }

    internal void Dispose() {
        _instance = null;
        _completeLoadTask?.SetCanceled();
        _completeLoadTask = null;
        GC.SuppressFinalize(this);
    }
}