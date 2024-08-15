using JALib.Stream;
using JALib.Tools;
using JALib.Tools.ByteTool;

namespace JALib.API.Packets;

internal class DiscordUpdate : RequestPacket {

    private readonly long id;
    
    public DiscordUpdate(long id) {
        this.id = id;
    }
    
    public override void ReceiveData(ByteArrayDataInput input) {
    }

    public override void GetBinary(ByteArrayDataOutput output) {
        output.WriteLong(id);
    }
}