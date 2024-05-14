package kr.jongyeol.jaServer.packet.request.admin;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.DiscordUserData;
import kr.jongyeol.jaServer.data.ModData;
import kr.jongyeol.jaServer.data.RawMod;
import kr.jongyeol.jaServer.data.Version;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import lombok.Cleanup;

public class RequestMods extends RequestPacket {

    private byte[] data;

    public static RequestMods AddRequestMod(DiscordUserData userData, RawMod mod) {
        RequestMods packet = new RequestMods();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeLong(userData.steamID);
        output.writeByte((byte) 0);
        output.writeUTF(mod.mod.getName());
        output.writeUTF(mod.version.toString());
        packet.data = output.toByteArray();
        return packet;
    }

    public static RequestMods RemoveRequestMod(DiscordUserData userData, int i) {
        RequestMods packet = new RequestMods();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeLong(userData.steamID);
        output.writeByte((byte) 1);
        output.writeInt(i);
        packet.data = output.toByteArray();
        return packet;
    }

    public static RequestMods ChangeRequestMod(DiscordUserData userData, int i, RawMod mod) {
        RequestMods packet = new RequestMods();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeLong(userData.steamID);
        output.writeByte((byte) 2);
        output.writeInt(i);
        output.writeUTF(mod.mod.getName());
        output.writeUTF(mod.version.toString());
        packet.data = output.toByteArray();
        return packet;
    }

    public static RequestMods ResetRequestMods(DiscordUserData userData) {
        RequestMods packet = new RequestMods();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeLong(userData.steamID);
        output.writeByte((byte) 3);
        packet.data = output.toByteArray();
        return packet;
    }

    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(data);
        DiscordUserData userData = DiscordUserData.getUserData(input.readLong());
        switch(input.readByte()) {
            case 0 -> {
                RawMod mod = new RawMod(ModData.getModData(input.readUTF()), new Version(input.readUTF()));
                userData.addRequestMod(mod);
                Connection.connections.stream().filter(c -> c.connectInfo.steamID == userData.steamID).forEach(Connection::loadModRequest);
                connection.logger.info("RequestMods Added: " + userData.steamID + " " + mod.mod.getName() + " " + mod.version);
            }
            case 1 -> {
                int i = input.readInt();
                userData.removeRequestMod(i);
                connection.logger.info("RequestMods Removed: " + userData.steamID + " " + i);
            }
            case 2 -> {
                int i = input.readInt();
                RawMod mod = new RawMod(ModData.getModData(input.readUTF()), new Version(input.readUTF()));
                userData.changeRequestMod(i, mod);
                connection.logger.info("RequestMods Changed: " + userData.steamID + " " + i + " " + mod.mod.getName() + " " + mod.version);
            }
            case 3 -> {
                userData.resetRequestMods();
                connection.logger.info("RequestMods Reset: " + userData.steamID);
            }
        }
    }

    @Override
    public byte[] getBinary() throws Exception {
        return data;
    }
}
