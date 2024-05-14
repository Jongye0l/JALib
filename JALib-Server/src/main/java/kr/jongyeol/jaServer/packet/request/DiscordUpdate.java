package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.UserData;
import kr.jongyeol.jaServer.packet.RequestPacket;

public class DiscordUpdate extends RequestPacket {
    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        long id = (long) data[0] << 56
                + (long) data[1] << 48
                + (long) data[2] << 40
                + (long) data[3] << 32
                + (long) data[4] << 24
                + (long) data[5] << 16
                + (long) data[6] << 8
                + (long) data[7];
        connection.connectInfo.discordID = id;
        connection.logger.info("Discord ID Update(id:" + id + ")");
        UserData.addDiscordID(connection.connectInfo.steamID, id);
        connection.loadModRequest();
    }

    @Override
    public byte[] getBinary() {
        return new byte[0];
    }
}
