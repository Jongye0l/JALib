using System.IO;

namespace JALib.API;

abstract class RequestPacket : Request {
    public long ID;
    public abstract void GetBinary(Stream output);
}