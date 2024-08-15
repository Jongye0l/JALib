package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.UserData;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import lombok.Cleanup;

public class ConnectInfo extends RequestPacket {
    public String libVer;
    public String adofaiVer;
    public int releaseNumber;
    public String steamBranchName;
    public long discordID;
    public long steamID;

    @Override
    public void getData(Connection connection, ByteArrayDataInput input) throws Exception {
        libVer = input.readUTF();
        adofaiVer = input.readUTF();
        releaseNumber = input.readInt();
        steamBranchName = input.readUTF();
        discordID = input.readLong();
        steamID = input.readLong();
        connection.connectInfo = this;
        connection.logger.info("Connect Info Update(steam:" + steamID + ", discord:" + discordID + ")");
        if(discordID != -1) UserData.addDiscordID(steamID, discordID);
        connection.loadModRequest();
    }

    @Override
    public void getBinary(ByteArrayDataOutput output) {
    }
}
