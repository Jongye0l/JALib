using System.Net.Http;
using System.Threading.Tasks;

namespace JALib.API;

abstract class RequestAPI : Request {
    public abstract Task Run(HttpClient client, string url);
}