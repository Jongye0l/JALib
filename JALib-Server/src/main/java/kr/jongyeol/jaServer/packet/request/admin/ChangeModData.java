package kr.jongyeol.jaServer.packet.request.admin;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.data.DownloadLink;
import kr.jongyeol.jaServer.data.ForceUpdateHandle;
import kr.jongyeol.jaServer.data.ModData;
import kr.jongyeol.jaServer.data.Version;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import lombok.Cleanup;

public class ChangeModData extends RequestPacket {

    private byte[] data;

    public static ChangeModData SetVersion(ModData modData, Version version) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 0);
        output.writeUTF(version.toString());
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData SetBetaVersion(ModData modData, Version version) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 1);
        output.writeUTF(version.toString());
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData SetForceUpdate(ModData modData, boolean forceUpdate) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 2);
        output.writeBoolean(forceUpdate);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData SetForceUpdateHandles(ModData modData, ForceUpdateHandle[] handles) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 3);
        output.writeInt(handles.length);
        for(ForceUpdateHandle handle : handles) handle.write(output);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData AddForceUpdateHandle(ModData modData, ForceUpdateHandle handle) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 4);
        handle.write(output);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData RemoveForceUpdateHandle(ModData modData, int i) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 5);
        output.writeInt(i);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData ChangeForceUpdateHandle(ModData modData, int i, ForceUpdateHandle handle) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 6);
        output.writeInt(i);
        handle.write(output);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData SetHomepage(ModData modData, String homepage) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 7);
        output.writeBoolean(homepage != null);
        if(homepage != null) output.writeUTF(homepage);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData SetDownloadLink(ModData modData, DownloadLink downloadLink) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 8);
        downloadLink.write(output);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData SetGid(ModData modData, int gid) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 9);
        output.writeInt(gid);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData LoadLocalizations(ModData modData) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 10);
        packet.data = output.toByteArray();
        return packet;
    }

    public static ChangeModData SetDiscord(ModData modData, String discord) {
        ChangeModData packet = new ChangeModData();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeUTF(modData.getName());
        output.writeByte((byte) 11);
        output.writeBoolean(discord != null);
        if(discord != null) output.writeUTF(discord);
        packet.data = output.toByteArray();
        return packet;
    }

    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(data);
        ModData modData = ModData.getModData(input.readUTF());
        switch(input.readByte()) {
            case 0 -> {
                Version version = new Version(input.readUTF());
                modData.setVersion(version);
                connection.logger.info(modData.getName() + " version changed to " + version);
            }
            case 1 -> {
                Version version = new Version(input.readUTF());
                modData.setBetaVersion(version);
                connection.logger.info(modData.getName() + " betaVersion changed to " + version);
            }
            case 2 -> {
                boolean forceUpdate = input.readBoolean();
                modData.setForceUpdate(forceUpdate);
                connection.logger.info(modData.getName() + " forceUpdate changed to " + forceUpdate);
            }
            case 3 -> {
                ForceUpdateHandle[] handles = new ForceUpdateHandle[input.readInt()];
                for(int i = 0; i < handles.length; i++) handles[i] = new ForceUpdateHandle(input);
                modData.setForceUpdateHandles(handles);
                connection.logger.info(modData.getName() + " forceUpdateHandles changed");
            }
            case 4 -> {
                modData.addForceUpdateHandles(new ForceUpdateHandle(input));
                connection.logger.info(modData.getName() + " forceUpdateHandles added");
            }
            case 5 -> {
                int i = input.readInt();
                modData.removeForceUpdateHandles(i);
                connection.logger.info(modData.getName() + " forceUpdateHandles removed at " + i);
            }
            case 6 -> {
                int i = input.readInt();
                modData.changeForceUpdateHandles(i, new ForceUpdateHandle(input));
                connection.logger.info(modData.getName() + " forceUpdateHandles changed at " + i);
            }
            case 7 -> {
                String homepage = input.readBoolean() ? input.readUTF() : null;
                modData.setHomepage(homepage);
                connection.logger.info(modData.getName() + " homepage changed to " + homepage);
            }
            case 8 -> {
                modData.setDownloadLink(DownloadLink.createDownloadLink(modData, input));
                connection.logger.info(modData.getName() + " downloadLink changed");
            }
            case 9 -> {
                int gid = input.readInt();
                modData.setGid(gid);
                connection.logger.info(modData.getName() + " gid changed to " + gid);
            }
            case 10 -> {
                modData.loadLocalizations();
                connection.logger.info(modData.getName() + " localizations loaded");
            }
            case 11 -> {
                String discord = input.readBoolean() ? input.readUTF() : null;
                modData.setDiscord(discord);
                connection.logger.info(modData.getName() + " discord changed to " + discord);
            }
        }
    }

    @Override
    public byte[] getBinary() throws Exception {
        return data;
    }
}
