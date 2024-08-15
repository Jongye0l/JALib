using System;
using System.Net.Http;
using JALib.Core;
using JALib.Stream;
using JALib.Tools;

namespace JALib.API.Packets;

internal class GetModInfo : RequestAPI {

    private JAMod mod;

    public GetModInfo(JAMod mod) {
        this.mod = mod;
    }
    
    public override void ReceiveData(ByteArrayDataInput input) {
        if(input.ReadBoolean()) mod.ModInfo(input);
    }

    public override async void Run(HttpClient client, string url) {
        try {
            System.IO.Stream stream = await client.GetStreamAsync(url + $"modInfo/{mod.Name}/{mod.Version}/{(mod.IsBetaBranch ? 1 : 0)}");
            using ByteArrayDataInput input = new(stream, JALib.Instance);
            ReceiveData(input);
        } catch (Exception e) {
            JALib.Instance.Log("Failed to connect to the server: " + url);
            JALib.Instance.LogException(e);
        }
    }
}