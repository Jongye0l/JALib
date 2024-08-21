using System.IO;
using JALib.Tools.ByteTool;

namespace JALib.API.Packets;

class DiscordUpdate : RequestPacket {

    private readonly long id;

    public DiscordUpdate(long id) {
        this.id = id;
    }

    public override void ReceiveData(Stream input) {
    }

    public override void GetBinary(Stream output) {
        output.WriteLong(id);
    }
}