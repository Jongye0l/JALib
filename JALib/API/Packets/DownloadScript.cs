using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JALib.Tools;

namespace JALib.API.Packets;

class DownloadScript(string name) : RequestAPI {

    public override void ReceiveData(Stream input) {
        throw new NotImplementedException();
    }

    public override async Task Run(HttpClient client, string url) {
        try {
            Stream stream = await client.GetStreamAsync(url + $"downloadScript/{name}");
            Zipper.Unzip(stream, Path.Combine(JALib.Instance.Path, "Scripts", name));
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + url);
            JALib.Instance.LogException(e);
        }
    }
}