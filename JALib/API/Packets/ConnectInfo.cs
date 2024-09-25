using System;
using System.IO;
using JALib.Tools.ByteTool;
using Steamworks;
using UnityEngine;
using Version = System.Version;

namespace JALib.API.Packets;

class ConnectInfo : AsyncRequestPacket {

    public override void ReceiveData(Stream input) {
        int size = input.ReadInt();
        for(int i = 0; i < size; i++) {
            string mod = input.ReadUTF();
            Version version = new(input.ReadUTF());
            JALib.DownloadMod(mod, version);
        }
    }

    public override void GetBinary(Stream output) {
        output.WriteUTF(JALib.Instance.Version.ToString());
        output.WriteUTF(Application.version);
        output.WriteInt(GCNS.releaseNumber);
        output.WriteUTF(GCS.steamBranchName ?? "Unknown");
        output.WriteLong(DiscordController.currentUserID);
        ulong steamID;
        try {
            steamID = SteamUser.GetSteamID().m_SteamID;
        } catch (Exception e) {
            steamID = 0;
            JALib.Instance.Log("Failed to get SteamID");
            JALib.Instance.LogException(e);
        }
        output.WriteULong(steamID);
    }
}