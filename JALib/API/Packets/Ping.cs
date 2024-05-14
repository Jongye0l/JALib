using System;

namespace JALib.API.Packets;

internal class Ping : AsyncRequestPacket {

    private long time;
    public int ping;
    
    public override void ReceiveData(byte[] data) {
        ping = (int) (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - time);
    }

    public override byte[] GetBinary() {
        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Array.Empty<byte>();
    }
}