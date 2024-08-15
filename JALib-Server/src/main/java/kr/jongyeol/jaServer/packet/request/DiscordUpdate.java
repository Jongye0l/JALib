package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.UserData;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;

public class DiscordUpdate extends RequestPacket {
    @Override
    public void getData(Connection connection, ByteArrayDataInput input) throws Exception {
        long id = input.readLong();
        connection.connectInfo.discordID = id;
        connection.logger.info("Discord ID Update(id:" + id + ")");
        UserData.addDiscordID(connection.connectInfo.steamID, id);
        connection.loadModRequest();
    }

    @Override
    public void getBinary(ByteArrayDataOutput output) {
    }
}
