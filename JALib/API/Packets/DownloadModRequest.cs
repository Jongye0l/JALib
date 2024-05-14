using System;
using JALib.Core;
using JALib.Stream;

namespace JALib.API.Packets;

internal class DownloadModRequest : ResponsePacket {
    public override void ReceiveData(byte[] data) {
        using ByteArrayDataInput input = new(data, JALib.Instance);
        string modName = input.ReadUTF();
        Version version = new(input.ReadUTF());
        JAMod mod = JAMod.GetMods(modName);
        if(mod != null && mod.Version < version) JAWebApi.DownloadMod(mod, false);
        else if(mod == null) JAWebApi.DownloadMod(modName);
    }
}