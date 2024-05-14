using JALib.Stream;

namespace JALib.API.Packets;

internal class PacketResponseError : ResponsePacket {
    public override void ReceiveData(byte[] data) {
        using ByteArrayDataInput input = new(data, JALib.Instance);
        JApi.ResponseError(input.ReadLong(), input.ReadUTF());
    }
}