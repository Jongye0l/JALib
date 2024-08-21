using System.IO;
using JALib.Tools.ByteTool;

namespace JALib.API.Packets;

class PacketResponseError : ResponsePacket {
    public override void ReceiveData(Stream input) {
        JApi.Instance.ResponseError(input.ReadLong(), input.ReadUTF());
    }
}