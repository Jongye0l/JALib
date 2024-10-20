package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;

public class DiscordUpdate extends RequestPacket {
    @Override
    public void getData(Connection connection, ByteArrayDataInput input) throws Exception {
    }

    @Override
    public void getBinary(ByteArrayDataOutput output) {
    }
}
