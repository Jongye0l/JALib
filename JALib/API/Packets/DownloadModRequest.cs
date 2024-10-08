﻿using System;
using System.IO;
using JALib.Core;
using JALib.Tools.ByteTool;

namespace JALib.API.Packets;

class DownloadModRequest : ResponsePacket {
    public override void ReceiveData(Stream input) {
        string modName = input.ReadUTF();
        Version version = new(input.ReadUTF());
        JAMod mod = JAMod.GetMods(modName);
        if(mod != null && mod.Version < version) JApi.Send(new DownloadMod(modName, version, mod.Path));
        else if(mod == null) JApi.Send(new DownloadMod(modName, version));
    }
}