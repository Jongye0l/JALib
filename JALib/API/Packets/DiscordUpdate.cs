using JALib.Tools;
using JALib.Tools.ByteTool;

namespace JALib.API.Packets;

internal class DiscordUpdate : RequestPacket {

    private readonly long id;
    
    public DiscordUpdate(long id) {
        this.id = id;
    }
    
    public override void ReceiveData(byte[] data) {
    }

    public override byte[] GetBinary() {
        return id.ToBytes();
    }
}