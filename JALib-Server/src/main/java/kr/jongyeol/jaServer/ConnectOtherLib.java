package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.data.*;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.Cleanup;
import org.springframework.http.HttpMethod;
import org.springframework.web.client.RestTemplate;

public class ConnectOtherLib {
    private static final RestTemplate restTemplate = new RestTemplate();
    private static final Logger logger = new Logger("ConnectOtherLib");

    public static void addModData(ModData modData) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeUTF(modData.getVersion().toString());
            output.writeUTF(modData.getBetaVersion().toString());
            output.writeBoolean(modData.isForceUpdate());
            ForceUpdateHandle[] handles = modData.getForceUpdateHandles();
            output.writeInt(handles.length);
            for(ForceUpdateHandle handle : handles) handle.write(output);
            output.writeBoolean(modData.getHomepage() != null);
            if(modData.getHomepage() != null) output.writeUTF(modData.getHomepage());
            modData.getDownloadLink().write(output);
            output.writeInt(modData.getGid());
            restTemplate.headForHeaders(Settings.getInstance().getOtherLibURL()).add("token", TokenData.getTokens().get(0));
            restTemplate.put(Settings.getInstance().getOtherLibURL() + "admin/modData", Compress.compress(output.toByteArray()));
        } catch (Exception e) {
            logger.error(e.getMessage());
        }
    }

    public static void setVersion(ModData modData, Version version) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 0);
            output.writeUTF(version.toString());
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setBetaVersion(ModData modData, Version version) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 1);
            output.writeUTF(version.toString());
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setForceUpdate(ModData modData, boolean forceUpdate) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 2);
            output.writeBoolean(forceUpdate);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setForceUpdateHandles(ModData modData, ForceUpdateHandle[] handles) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 3);
            output.writeInt(handles.length);
            for(ForceUpdateHandle handle : handles) handle.write(output);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void addForceUpdateHandle(ModData modData, ForceUpdateHandle handle) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 4);
            handle.write(output);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void removeForceUpdateHandle(ModData modData, int i) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 5);
            output.writeInt(i);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void changeForceUpdateHandle(ModData modData, int i, ForceUpdateHandle handle) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 6);
            output.writeInt(i);
            handle.write(output);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setHomepage(ModData modData, String homepage) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 7);
            output.writeUTF(homepage);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setDownloadLink(ModData modData, DownloadLink downloadLink) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 8);
            downloadLink.write(output);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setGid(ModData modData, int gid) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 9);
            output.writeInt(gid);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void loadLocalizations(ModData modData) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 10);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setDiscord(ModData modData, String discord) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 11);
            output.writeUTF(discord);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    private static void changeModData(byte[] data) {
        try {
            restTemplate.headForHeaders(Settings.getInstance().getOtherLibURL()).add("token", TokenData.getTokens().get(0));
            restTemplate.patchForObject(Settings.getInstance().getOtherLibURL() + "admin/modData", Compress.compress(data), String.class);
        } catch (Exception e) {
            logger.error(e.getMessage());
        }
    }

    public static void setupModData() {
        try {
            restTemplate.headForHeaders(Settings.getInstance().getOtherLibURL()).add("token", TokenData.getTokens().get(0));
            byte[] data = restTemplate.getForEntity(Settings.getInstance().getOtherLibURL() + "admin/modData", byte[].class).getBody();
            ByteArrayDataInput input = new ByteArrayDataInput(Compress.decompress(data));
            int length = input.readInt();
            for(int i = 0; i < length; i++) ModData.createMod(input.readUTF(), input);
        } catch (Exception e) {
            logger.error(e.getMessage());
        }
    }

    public static void addRequestMods(long discordId, RawMod rawMod) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeLong(discordId);
            output.writeUTF(rawMod.mod.getName());
            output.writeUTF(rawMod.version.toString());
            restTemplate.headForHeaders(Settings.getInstance().getOtherLibURL()).add("token", TokenData.getTokens().get(0));
            restTemplate.put(Settings.getInstance().getOtherLibURL() + "admin/requestMods", Compress.compress(output.toByteArray()));
        } catch (Exception e) {
            logger.error(e.getMessage());
        }
    }

    public static void removeRequestMods(long discordId, int i) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeLong(discordId);
            output.writeBoolean(false);
            output.writeInt(i);
            removeRequestMods(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void resetRequestMods(long discordId) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeLong(discordId);
            output.writeBoolean(true);
            removeRequestMods(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    private static void removeRequestMods(byte[] data) {
        try {
            restTemplate.headForHeaders(Settings.getInstance().getOtherLibURL()).add("token", TokenData.getTokens().get(0));
            restTemplate.execute(Settings.getInstance().getOtherLibURL() + "admin/requestMods", HttpMethod.DELETE,
                restTemplate.httpEntityCallback(Compress.compress(data)), null);
        } catch (Exception e) {
            logger.error(e.getMessage());
        }
    }

    public static void changeRequestMods(long discordId, int i, RawMod rawMod) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeLong(discordId);
            output.writeInt(i);
            output.writeUTF(rawMod.mod.getName());
            output.writeUTF(rawMod.version.toString());
            restTemplate.headForHeaders(Settings.getInstance().getOtherLibURL()).add("token", TokenData.getTokens().get(0));
            restTemplate.patchForObject(Settings.getInstance().getOtherLibURL() + "admin/requestMods", Compress.compress(output.toByteArray()), String.class);
        } catch (Exception e) {
            logger.error(e.getMessage());
        }
    }

    public static void loadModRequest() {
        try {
            restTemplate.headForHeaders(Settings.getInstance().getOtherLibURL()).add("token", TokenData.getTokens().get(0));
            byte[] data = restTemplate.getForEntity(Settings.getInstance().getOtherLibURL() + "admin/requestMods", byte[].class).getBody();
            ByteArrayDataInput input = new ByteArrayDataInput(Compress.decompress(data));
            for(DiscordUserData userData : DiscordUserData.getUserData()) {
                userData.resetRequestMods();
                int length = input.readInt();
                for(int i = 0; i < length; i++) {
                    RawMod mod = new RawMod(ModData.getModData(input.readUTF()), new Version(input.readUTF()));
                    userData.addRequestMod(mod);
                }
            }
        } catch (Exception e) {
            logger.error(e.getMessage());
        }
    }
}
