using System.Net.Http;

namespace JALib.API;

internal abstract class RequestAPI : Request {
    public abstract void Run(HttpClient client, string url);
}