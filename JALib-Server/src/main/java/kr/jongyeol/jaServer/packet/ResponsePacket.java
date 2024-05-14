package kr.jongyeol.jaServer.packet;

public abstract class ResponsePacket {
    public abstract byte[] getBinary() throws Exception;
}
