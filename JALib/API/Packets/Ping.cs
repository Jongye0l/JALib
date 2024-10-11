using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace JALib.API.Packets;

class Ping : RequestAPI {

    private long time;
    public int ping;

    public override void ReceiveData(Stream input) {
        ping = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - time);
    }

    public override async Task Run(HttpClient client, string url) {
        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await client.GetAsync(url + "ping");
        ReceiveData(null);
    }
}