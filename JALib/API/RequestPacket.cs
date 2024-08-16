using JALib.Stream;

namespace JALib.API;

abstract class RequestPacket : Request {
    public long ID;
    public abstract void GetBinary(ByteArrayDataOutput output);
}