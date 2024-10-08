package kr.jongyeol.jaServer.packet.request;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.DiscordUserData;
import kr.jongyeol.jaServer.data.RawMod;
import kr.jongyeol.jaServer.data.UserData;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;

import java.util.ArrayList;
import java.util.List;

public class ConnectInfo extends RequestPacket {
    public String libVer;
    public String adofaiVer;
    public int releaseNumber;
    public String steamBranchName;
    public long discordID;
    public long steamID;
    public List<RawMod> requestMods;

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
        if(discordID != -1 && steamID != 0) UserData.addDiscordID(steamID, discordID);
        requestMods = new ArrayList<>();
        if(steamID == 0) if(discordID != -1) requestMods.addAll(List.of(DiscordUserData.getUserData(discordID).getRequestMods()));
        else {
            UserData.getUserData(steamID).forEach(l -> {
                try {
                    requestMods.addAll(List.of(DiscordUserData.getUserData(l).getRequestMods()));
                } catch (Exception e) {
                    if(connection.logger != null) connection.logger.error(e);
                }
            });
        }
    }

    @Override
    public void getBinary(ByteArrayDataOutput output) {
        output.writeInt(requestMods.size());
        for(RawMod mod : requestMods) {
            output.writeUTF(mod.mod.getName());
            output.writeUTF(mod.version.toString());
        }
        requestMods = null;
    }
}
