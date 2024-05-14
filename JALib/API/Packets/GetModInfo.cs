using JALib.Core;
using JALib.Stream;

namespace JALib.API.Packets;

internal class GetModInfo : RequestPacket {

    private JAMod mod;
    
    public GetModInfo(JAMod mod) {
        this.mod = mod;
    }
    
    public override void ReceiveData(byte[] data) {
        using ByteArrayDataInput input = new(data, JALib.Instance);
        mod.ModInfo(input);
    }

    public override byte[] GetBinary() {
        using ByteArrayDataOutput output = new(JALib.Instance);
        output.WriteUTF(mod.Name);
        output.WriteUTF(mod.Version.ToString());
        output.WriteBoolean(mod.IsBetaBranch);
        return output.ToByteArray();
    }
}