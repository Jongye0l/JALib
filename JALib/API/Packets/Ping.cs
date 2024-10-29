using System.Net.Http;
using System.Threading.Tasks;

namespace JALib.API.Packets;

class Ping : GetRequest {

    private long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public int ping;

    public override string UrlBehind => "ping";

    public override Task Run(HttpResponseMessage message) {
        ping = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - time);
        return Task.CompletedTask;
    }
}