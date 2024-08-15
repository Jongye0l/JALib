using JALib.Stream;

namespace JALib.API;

internal abstract class ResponsePacket {
    public abstract void ReceiveData(ByteArrayDataInput input);
}