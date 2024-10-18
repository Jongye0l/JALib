using System.Net.Http;
using System.Threading.Tasks;

namespace JALib.API;

abstract class GetRequest {
    public abstract string UrlBehind { get; }

    public abstract Task Run(HttpResponseMessage message);
}