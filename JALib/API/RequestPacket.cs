namespace JALib.API;

internal abstract class RequestPacket : ResponsePacket {
    public long ID;
    public abstract byte[] GetBinary();
}