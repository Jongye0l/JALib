using System;
using JALib.Core;
using JALib.Stream;

namespace JALib.API.Packets;

class DownloadModRequest : ResponsePacket {
    public override void ReceiveData(ByteArrayDataInput input) {
        string modName = input.ReadUTF();
        Version version = new(input.ReadUTF());
        JAMod mod = JAMod.GetMods(modName);
        if(mod != null && mod.Version < version) JAWebAPI.DownloadMod(mod, false);
        else if(mod == null) JAWebAPI.DownloadMod(modName);
    }
}