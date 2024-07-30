using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JALib.Tools.ByteTool;

namespace JALib.Tools;

public static class SimpleHttp {
    public static async Task<byte[]> Get(this HttpClient httpClient, string url) => await httpClient.GetByteArrayAsync(url);
    
    public static async Task<string> GetString(this HttpClient httpClient, string url) => await httpClient.GetStringAsync(url);
    
    public static async Task<byte[]> Post(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PostAsync(url, new ByteArrayContent(data))).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> PostString(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PostAsync(url, new ByteArrayContent(data))).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Post(this HttpClient httpClient, string url, string data) => await (await httpClient.PostAsync(url, new StringContent(data))).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> PostString(this HttpClient httpClient, string url, string data) => await (await httpClient.PostAsync(url, new StringContent(data))).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Post(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PostAsync(url, data)).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> PostString(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PostAsync(url, data)).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Put(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PutAsync(url, new ByteArrayContent(data))).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> PutString(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PutAsync(url, new ByteArrayContent(data))).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Put(this HttpClient httpClient, string url, string data) => await (await httpClient.PutAsync(url, new StringContent(data))).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> PutString(this HttpClient httpClient, string url, string data) => await (await httpClient.PutAsync(url, new StringContent(data))).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Put(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PutAsync(url, data)).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> PutString(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PutAsync(url, data)).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Delete(this HttpClient httpClient, string url) => await (await httpClient.DeleteAsync(url)).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> DeleteString(this HttpClient httpClient, string url) => await (await httpClient.DeleteAsync(url)).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request) => await (await httpClient.SendAsync(request)).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request) => await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption) => 
        await (await httpClient.SendAsync(request, completionOption)).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption) => 
        await (await httpClient.SendAsync(request, completionOption)).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, completionOption, cancellationToken)).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, completionOption, cancellationToken)).Content.ReadAsStringAsync();
    
    public static async Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, cancellationToken)).Content.ReadAsByteArrayAsync();
    
    public static async Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, cancellationToken)).Content.ReadAsStringAsync();

    public static async Task<byte[]> Get(this WebClient webClient, string url) => await webClient.DownloadDataTaskAsync(url);

    public static async Task<string> GetString(this WebClient webClient, string url) => await webClient.DownloadStringTaskAsync(url);

    public static async Task<byte[]> Post(this WebClient webClient, string url, byte[] data) => await webClient.UploadDataTaskAsync(url, data);

    public static async Task<string> PostString(this WebClient webClient, string url, byte[] data) => Encoding.UTF8.GetString(await webClient.UploadDataTaskAsync(url, data));

    public static async Task<byte[]> Post(this WebClient webClient, string url, string data) => (await webClient.UploadStringTaskAsync(url, data)).ToBytes();

    public static async Task<string> PostString(this WebClient webClient, string url, string data) => await webClient.UploadStringTaskAsync(url, data);

    public static async Task<byte[]> Put(this WebClient webClient, string url, byte[] data) => await webClient.UploadDataTaskAsync(url, "PUT", data);

    public static async Task<string> PutString(this WebClient webClient, string url, byte[] data) => Encoding.UTF8.GetString(await webClient.UploadDataTaskAsync(url, "PUT", data));

    public static async Task<byte[]> Put(this WebClient webClient, string url, string data) => (await webClient.UploadStringTaskAsync(url, "PUT", data)).ToBytes();

    public static async Task<string> PutString(this WebClient webClient, string url, string data) => await webClient.UploadStringTaskAsync(url, "PUT", data);

    public static async Task<byte[]> Delete(this WebClient webClient, string url) => await webClient.UploadDataTaskAsync(url, "DELETE", Array.Empty<byte>());

    public static async Task<string> DeleteString(this WebClient webClient, string url) => Encoding.UTF8.GetString(await webClient.UploadDataTaskAsync(url, "DELETE", Array.Empty<byte>()));

    public static async Task<byte[]> Send(this WebClient webClient, string method, string url, byte[] data) => await webClient.UploadDataTaskAsync(url, method, data);

    public static async Task<string> SendString(this WebClient webClient, string method, string url, byte[] data) => Encoding.UTF8.GetString(await webClient.UploadDataTaskAsync(url, method, data));

    public static async Task<byte[]> Send(this WebClient webClient, string method, string url, string data) => (await webClient.UploadStringTaskAsync(url, method, data)).ToBytes();

    public static async Task<string> SendString(this WebClient webClient, string method, string url, string data) => await webClient.UploadStringTaskAsync(url, method, data);
}