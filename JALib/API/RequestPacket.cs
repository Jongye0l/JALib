using JALib.Stream;

namespace JALib.API;

internal abstract class RequestPacket : Request {
    public long ID;
    public abstract void GetBinary(ByteArrayDataOutput output);
}