using JALib.Stream;

namespace JALib.API.Packets;

internal class Status : RequestPacket {

    public int Ping;
    public long[] NotComplete;
    
    public Status(int ping, long[] notComplete) {
        Ping = ping;
        NotComplete = notComplete;
    }
    
    public override void ReceiveData(byte[] data) {
        
    }

    public override byte[] GetBinary() {
        using ByteArrayDataOutput output = new();
        output.WriteInt(Ping);
        output.WriteInt(NotComplete.Length);
        foreach(long l in NotComplete) output.WriteLong(l);
        return output.ToByteArray();
    }
}