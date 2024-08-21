using System.IO;

namespace JALib.API;

abstract class ResponsePacket {
    public abstract void ReceiveData(Stream input);
}