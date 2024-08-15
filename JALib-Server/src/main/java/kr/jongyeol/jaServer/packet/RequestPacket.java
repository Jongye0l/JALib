package kr.jongyeol.jaServer.packet;

import kr.jongyeol.jaServer.Connection;

public abstract class RequestPacket extends ResponsePacket {
    public long id;
    public abstract void getData(Connection connection, ByteArrayDataInput input) throws Exception;
}
