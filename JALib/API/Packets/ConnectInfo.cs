using System;
using System.IO;
using JALib.Tools.ByteTool;
using Steamworks;
using UnityEngine;

namespace JALib.API.Packets;

class ConnectInfo : RequestPacket {

    public override void ReceiveData(Stream input) {
    }

    public override void GetBinary(Stream output) {
        output.WriteUTF(JALib.Instance.Version.ToString());
        output.WriteUTF(Application.version);
        output.WriteInt(GCNS.releaseNumber);
        output.WriteUTF(GCS.steamBranchName);
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