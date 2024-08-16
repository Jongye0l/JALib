using JALib.Stream;

namespace JALib.API.Packets;

class PacketResponseError : ResponsePacket {
    public override void ReceiveData(ByteArrayDataInput input) {
        JApi.Instance.ResponseError(input.ReadLong(), input.ReadUTF());
    }
}