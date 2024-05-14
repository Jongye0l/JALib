package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.packet.RequestPacket;

public class Ping extends RequestPacket {

    @Override
    public void getData(Connection connection, byte[] data) {
        connection.logger.info("Ping");
    }

    @Override
    public byte[] getBinary() {
        return new byte[0];
    }
}
