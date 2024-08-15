package kr.jongyeol.jaServer.packet;

public abstract class ResponsePacket {
    public abstract void getBinary(ByteArrayDataOutput output) throws Exception;
}
