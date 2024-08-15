using JALib.Stream;
using Steamworks;
using UnityEngine;

namespace JALib.API.Packets;

internal class ConnectInfo : RequestPacket {

    public override void ReceiveData(ByteArrayDataInput input) {
    }

    public override void GetBinary(ByteArrayDataOutput output) {
        output.WriteUTF(JALib.Instance.Version.ToString());
        output.WriteUTF(Application.version);
        output.WriteInt(GCNS.releaseNumber);
        output.WriteUTF(GCS.steamBranchName);
        output.WriteLong(DiscordController.currentUserID);
        output.WriteULong(SteamUser.GetSteamID().m_SteamID);
    }
}