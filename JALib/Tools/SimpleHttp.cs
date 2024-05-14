using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JALib.Tools;

public static class SimpleHttp {
    public async static Task<byte[]> Get(this HttpClient httpClient, string url) => await httpClient.GetByteArrayAsync(url);
    
    public async static Task<string> GetString(this HttpClient httpClient, string url) => await httpClient.GetStringAsync(url);
    
    public async static Task<byte[]> Post(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PostAsync(url, new ByteArrayContent(data))).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> PostString(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PostAsync(url, new ByteArrayContent(data))).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Post(this HttpClient httpClient, string url, string data) => await (await httpClient.PostAsync(url, new StringContent(data))).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> PostString(this HttpClient httpClient, string url, string data) => await (await httpClient.PostAsync(url, new StringContent(data))).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Post(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PostAsync(url, data)).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> PostString(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PostAsync(url, data)).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Put(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PutAsync(url, new ByteArrayContent(data))).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> PutString(this HttpClient httpClient, string url, byte[] data) => await (await httpClient.PutAsync(url, new ByteArrayContent(data))).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Put(this HttpClient httpClient, string url, string data) => await (await httpClient.PutAsync(url, new StringContent(data))).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> PutString(this HttpClient httpClient, string url, string data) => await (await httpClient.PutAsync(url, new StringContent(data))).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Put(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PutAsync(url, data)).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> PutString(this HttpClient httpClient, string url, HttpContent data) => await (await httpClient.PutAsync(url, data)).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Delete(this HttpClient httpClient, string url) => await (await httpClient.DeleteAsync(url)).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> DeleteString(this HttpClient httpClient, string url) => await (await httpClient.DeleteAsync(url)).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request) => await (await httpClient.SendAsync(request)).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request) => await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption) => 
        await (await httpClient.SendAsync(request, completionOption)).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption) => 
        await (await httpClient.SendAsync(request, completionOption)).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, completionOption, cancellationToken)).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, completionOption, cancellationToken)).Content.ReadAsStringAsync();
    
    public async static Task<byte[]> Send(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, cancellationToken)).Content.ReadAsByteArrayAsync();
    
    public async static Task<string> SendString(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken) => 
        await (await httpClient.SendAsync(request, cancellationToken)).Content.ReadAsStringAsync();
}