using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JALib.Tools.ByteTool;

namespace JALib.Tools;

public static class SimpleHttp {
    public static Task<byte[]> Get(this HttpClient httpClient, string url) => httpClient.GetByteArrayAsync(url);

    public static Task<string> GetString(this HttpClient httpClient, string url) => httpClient.GetStringAsync(url);

    public static Task<byte[]> Post(this HttpClient httpClient, string url, byte[] data) => ReadBytes(httpClient.PostAsync(url, new ByteArrayContent(data)));

    public static Task<string> PostString(this HttpClient httpClient, string url, byte[] data) => ReadString(httpClient.PostAsync(url, new ByteArrayContent(data)));

    public static Task<byte[]> Post(this HttpClient httpClient, string url, string data) => ReadBytes(httpClient.PostAsync(url, new StringContent(data)));

    public static Task<string> PostString(this HttpClient httpClient, string url, string data) => ReadString(httpClient.PostAsync(url, new StringContent(data)));

    public static Task<byte[]> Post(this HttpClient httpClient, string url, HttpContent data) => ReadBytes(httpClient.PostAsync(url, data));

    public static Task<string> PostString(this HttpClient httpClient, string url, HttpContent data) => ReadString(httpClient.PostAsync(url, data));

    public static Task<byte[]> Put(this HttpClient httpClient, string url, byte[] data) => ReadBytes(httpClient.PutAsync(url, new ByteArrayContent(data)));

    public static Task<string> PutString(this HttpClient httpClient, string url, byte[] data) => ReadString(httpClient.PutAsync(url, new ByteArrayContent(data)));

    public static Task<byte[]> Put(this HttpClient httpClient, string url, string data) => ReadBytes(httpClient.PutAsync(url, new StringContent(data)));

    public static Task<string> PutString(this HttpClient httpClient, string url, string data) => ReadString(httpClient.PutAsync(url, new StringContent(data)));

    public static Task<byte[]> Put(this HttpClient httpClient, string url, HttpContent data) => ReadBytes(httpClient.PutAsync(url, data));

    public static Task<string> PutString(this HttpClient httpClient, string url, HttpContent data) => ReadString(httpClient.PutAsync(url, data));

    public static Task<byte[]> Delete(this HttpClient httpClient, string url) => ReadBytes(httpClient.DeleteAsync(url));

    public static Task<string> DeleteString(this HttpClient httpClient, string url) => ReadString(httpClient.DeleteAsync(url));

    public static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request) => ReadBytes(httpClient.SendAsync(request));

    public static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request) => ReadString(httpClient.SendAsync(request));

    public static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption) =>
        ReadBytes(httpClient.SendAsync(request, completionOption));

    public static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption) =>
        ReadString(httpClient.SendAsync(request, completionOption));

    public static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) =>
        ReadBytes(httpClient.SendAsync(request, completionOption, cancellationToken));

    public static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) =>
        ReadString(httpClient.SendAsync(request, completionOption, cancellationToken));

    public static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken) =>
        ReadBytes(httpClient.SendAsync(request, cancellationToken));

    public static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken) =>
        ReadString(httpClient.SendAsync(request, cancellationToken));

    public static Task<byte[]> Get(this WebClient webClient, string url) => webClient.DownloadDataTaskAsync(url);

    public static Task<string> GetString(this WebClient webClient, string url) => webClient.DownloadStringTaskAsync(url);

    public static Task<byte[]> Post(this WebClient webClient, string url, byte[] data) => webClient.UploadDataTaskAsync(url, data);

    public static Task<string> PostString(this WebClient webClient, string url, byte[] data) => webClient.UploadDataTaskAsync(url, data).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));

    public static Task<byte[]> Post(this WebClient webClient, string url, string data) => webClient.UploadStringTaskAsync(url, data).ContinueWith(buffer => buffer.Result.ToBytes());

    public static Task<string> PostString(this WebClient webClient, string url, string data) => webClient.UploadStringTaskAsync(url, data);

    public static Task<byte[]> Put(this WebClient webClient, string url, byte[] data) => webClient.UploadDataTaskAsync(url, "PUT", data);

    public static Task<string> PutString(this WebClient webClient, string url, byte[] data) => webClient.UploadDataTaskAsync(url, "PUT", data).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));

    public static Task<byte[]> Put(this WebClient webClient, string url, string data) => webClient.UploadStringTaskAsync(url, "PUT", data).ContinueWith(buffer => buffer.Result.ToBytes());

    public static Task<string> PutString(this WebClient webClient, string url, string data) => webClient.UploadStringTaskAsync(url, "PUT", data);

    public static Task<byte[]> Delete(this WebClient webClient, string url) => webClient.UploadDataTaskAsync(url, "DELETE", []);

    public static Task<string> DeleteString(this WebClient webClient, string url) => webClient.UploadDataTaskAsync(url, "DELETE", []).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));

    public static Task<byte[]> Send(this WebClient webClient, string method, string url, byte[] data) => webClient.UploadDataTaskAsync(url, method, data);

    public static Task<string> SendString(this WebClient webClient, string method, string url, byte[] data) => webClient.UploadDataTaskAsync(url, method, data).ContinueWith(buffer => Encoding.UTF8.GetString(buffer.Result));

    public static Task<byte[]> Send(this WebClient webClient, string method, string url, string data) => webClient.UploadStringTaskAsync(url, method, data).ContinueWith(buffer => buffer.Result.ToBytes());

    public static Task<string> SendString(this WebClient webClient, string method, string url, string data) => webClient.UploadStringTaskAsync(url, method, data);

    public static Task<byte[]> ReadBytes(this Task<HttpResponseMessage> task) => task.ContinueWith(t => t.Result.Content.ReadAsByteArrayAsync()).Unwrap();

    public static Task<string> ReadString(this Task<HttpResponseMessage> task) => task.ContinueWith(t => t.Result.Content.ReadAsStringAsync()).Unwrap();
}