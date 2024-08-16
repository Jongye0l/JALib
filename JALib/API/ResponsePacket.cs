using JALib.Stream;

namespace JALib.API;

abstract class ResponsePacket {
    public abstract void ReceiveData(ByteArrayDataInput input);
}