using System;
using JALib.Stream;
using Steamworks;
using UnityEngine;

namespace JALib.API.Packets;

class ConnectInfo : RequestPacket {

    public override void ReceiveData(ByteArrayDataInput input) {
    }

    public override void GetBinary(ByteArrayDataOutput output) {
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