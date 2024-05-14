namespace JALib.API;

internal abstract class ResponsePacket {
    public abstract void ReceiveData(byte[] data);
}