using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JALib.Tools.ByteTool;
using UnityEngine;

namespace JALib.Tools;

public static class SimpleHttp {
    extension(HttpClient httpClient) {
        public Task<byte[]> Get(string url) => httpClient.GetByteArrayAsync(url);
        public Task<string> GetString(string url) => httpClient.GetStringAsync(url);
        public Task<byte[]> Post(string url, byte[] data) => ReadBytes(httpClient.PostAsync(url, new ByteArrayContent(data)));
        public Task<string> PostString(string url, byte[] data) => ReadString(httpClient.PostAsync(url, new ByteArrayContent(data)));
        public Task<byte[]> Post(string url, string data) => ReadBytes(httpClient.PostAsync(url, new StringContent(data)));
        public Task<string> PostString(string url, string data) => ReadString(httpClient.PostAsync(url, new StringContent(data)));
        public Task<byte[]> Post(string url, HttpContent data) => ReadBytes(httpClient.PostAsync(url, data));
        public Task<string> PostString(string url, HttpContent data) => ReadString(httpClient.PostAsync(url, data));
        public Task<byte[]> Put(string url, byte[] data) => ReadBytes(httpClient.PutAsync(url, new ByteArrayContent(data)));
        public Task<string> PutString(string url, byte[] data) => ReadString(httpClient.PutAsync(url, new ByteArrayContent(data)));
        public Task<byte[]> Put(string url, string data) => ReadBytes(httpClient.PutAsync(url, new StringContent(data)));
        public Task<string> PutString(string url, string data) => ReadString(httpClient.PutAsync(url, new StringContent(data)));
        public Task<byte[]> Put(string url, HttpContent data) => ReadBytes(httpClient.PutAsync(url, data));
        public Task<string> PutString(string url, HttpContent data) => ReadString(httpClient.PutAsync(url, data));
        public Task<byte[]> Delete(string url) => ReadBytes(httpClient.DeleteAsync(url));
        public Task<string> DeleteString(string url) => ReadString(httpClient.DeleteAsync(url));
        public Task<byte[]> Send(HttpRequestMessage request) => ReadBytes(httpClient.SendAsync(request));
        public Task<string> SendString(HttpRequestMessage request) => ReadString(httpClient.SendAsync(request));
        public Task<byte[]> Send(HttpRequestMessage request, HttpCompletionOption completionOption) =>
            ReadBytes(httpClient.SendAsync(request, completionOption));
        public Task<string> SendString(HttpRequestMessage request, HttpCompletionOption completionOption) =>
            ReadString(httpClient.SendAsync(request, completionOption));
        public Task<byte[]> Send(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) =>
            ReadBytes(httpClient.SendAsync(request, completionOption, cancellationToken));
        public Task<string> SendString(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) =>
            ReadString(httpClient.SendAsync(request, completionOption, cancellationToken));
        public Task<byte[]> Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            ReadBytes(httpClient.SendAsync(request, cancellationToken));
        public Task<string> SendString(HttpRequestMessage request, CancellationToken cancellationToken) =>
            ReadString(httpClient.SendAsync(request, cancellationToken));
        public void SetupUserAgent(string appName, string appVersion) {
            string userAgent = $"{appName}/{appVersion} ({GetOSInfo()})";
            if(httpClient.DefaultRequestHeaders.UserAgent.ToString() == userAgent) return;
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }
    }

    extension(WebClient webClient) {
        public Task<byte[]> Get(string url) => webClient.DownloadDataTaskAsync(url);
        public Task<string> GetString(string url) => webClient.DownloadStringTaskAsync(url);
        public Task<byte[]> Post(string url, byte[] data) => webClient.UploadDataTaskAsync(url, data);
        public Task<string> PostString(string url, byte[] data) => webClient.UploadDataTaskAsync(url, data).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));
        public Task<byte[]> Post(string url, string data) => webClient.UploadStringTaskAsync(url, data).ContinueWith(buffer => buffer.Result.ToBytes());
        public Task<string> PostString(string url, string data) => webClient.UploadStringTaskAsync(url, data);
        public Task<byte[]> Put(string url, byte[] data) => webClient.UploadDataTaskAsync(url, "PUT", data);
        public Task<string> PutString(string url, byte[] data) => webClient.UploadDataTaskAsync(url, "PUT", data).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));
        public Task<byte[]> Put(string url, string data) => webClient.UploadStringTaskAsync(url, "PUT", data).ContinueWith(buffer => buffer.Result.ToBytes());
        public Task<string> PutString(string url, string data) => webClient.UploadStringTaskAsync(url, "PUT", data);
        public Task<byte[]> Delete(string url) => webClient.UploadDataTaskAsync(url, "DELETE", []);
        public Task<string> DeleteString(string url) => webClient.UploadDataTaskAsync(url, "DELETE", []).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));
        public Task<byte[]> Send(string method, string url, byte[] data) => webClient.UploadDataTaskAsync(url, method, data);
        public Task<string> SendString(string method, string url, byte[] data) => webClient.UploadDataTaskAsync(url, method, data).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));
        public Task<byte[]> Send(string method, string url, string data) => webClient.UploadStringTaskAsync(url, method, data).ContinueWith(buffer => buffer.Result.ToBytes());
        public Task<string> SendString(string method, string url, string data) => webClient.UploadStringTaskAsync(url, method, data);
        public void SetupUserAgent(string appName, string appVersion) {
            string userAgent = $"{appName}/{appVersion} ({GetOSInfo()})";
            webClient.Headers[HttpRequestHeader.UserAgent] = userAgent;
        }
    }

    extension(Task<HttpResponseMessage> task) {
        public Task<byte[]> ReadBytes() => task.ContinueWith(t => t.Result.Content.ReadAsByteArrayAsync()).Unwrap();
        public Task<string> ReadString() => task.ContinueWith(t => t.Result.Content.ReadAsStringAsync()).Unwrap();
    }
    
    public static string GetOSInfo() {
#if TEST
        return "Windows NT 10.0; Win64; x64";
#else
        string os = SystemInfo.operatingSystem;
        Match m;
        string ver;
        Version version;

        if(os.Contains("Windows")) {
            m = Regex.Match(os, @"\(([\d\.]+)\) (\d+)bit");
            if(m.Success) {
                version = new Version(m.Groups[1].Value);
                ver = version.Major + "." + version.Minor;
            } else ver = "10.0";
            int bit = m.Success && int.TryParse(m.Groups[2].Value, out int b) ? b : 64;
            return $"Windows NT {ver}; " + (bit == 64 ? "Win64; x64" : "WOW64");
        }
        if(os.Contains("Linux")) {
            m = Regex.Match(os, @"Linux\s+([\d\.]+)");
            if(m.Success) {
                version = new Version(m.Groups[1].Value);
                ver = version.Major + "." + version.Minor;
            } else ver = "5.0";
            return $"X11; Linux {ver} x86_64";
        }
        if(os.Contains("Mac OS")) {
            m = Regex.Match(os, @"Mac OS X (\d+[\._]\d+[\._]?\d*)");
            ver = m.Success ? m.Groups[1].Value.Replace('_', '.') : "10.15.7";
            return $"Macintosh; Intel Mac OS X {ver}";
        }
        if(os.Contains("Android")) {
            m = Regex.Match(os, @"Android OS (\d+)");
            ver = m.Success ? m.Groups[1].Value : "10";
            return $"Linux; Android {ver}";
        }
        if(os.Contains("iOS")) {
            m = Regex.Match(os, @"iOS (\d+(\.\d+)*)");
            ver = m.Success ? m.Groups[1].Value : "16.0";
            return $"iPhone; CPU iPhone OS {ver.Replace('.', '_')} like Mac OS X";
        }
        return "Unknown";
#endif
    }

}