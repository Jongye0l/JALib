using System.Net.Http;

namespace JALib.API;

abstract class RequestAPI : Request {
    public abstract void Run(HttpClient client, string url);
}