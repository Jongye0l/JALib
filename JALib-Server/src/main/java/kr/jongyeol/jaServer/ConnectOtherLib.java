package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.data.*;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.Cleanup;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.web.client.HttpStatusCodeException;
import org.springframework.web.client.RestTemplate;

import java.util.Map;

public class ConnectOtherLib {
    private static final RestTemplate restTemplate = new RestTemplate();
    private static final Logger logger = Logger.createLogger("ConnectOtherLib");

    public static void addModData(ModData modData) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            modToBytes(output, modData);
            HttpHeaders headers = new HttpHeaders();
            headers.add("token", TokenData.getTokens().get(0));
            HttpEntity<byte[]> entity = new HttpEntity<>(GZipFile.gzipData(output.toByteArray()), headers);
            restTemplate.exchange(Settings.getInstance().getOtherLibURL() + "admin/modData", HttpMethod.POST, entity, String.class);
        } catch (HttpStatusCodeException e) {
            logger.error(e.getMessage());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void modToBytes(ByteArrayDataOutput output, ModData modData) {
        output.writeUTF(modData.getName());
        output.writeUTF(modData.getVersion() == null ? null : modData.getVersion().toString());
        output.writeUTF(modData.getBetaVersion() == null ? null : modData.getBetaVersion().toString());
        output.writeBoolean(modData.isForceUpdate());
        output.writeBoolean(modData.isForceUpdateBeta());
        Map<Version, Boolean> betaMap = modData.getBetaMap();
        output.writeInt(betaMap.size());
        for(Map.Entry<Version, Boolean> entry : betaMap.entrySet()) {
            output.writeUTF(entry.getKey().toString());
            output.writeBoolean(entry.getValue());
        }
        ForceUpdateHandle[] handles = modData.getForceUpdateHandles();
        output.writeInt(handles.length);
        for(ForceUpdateHandle handle : handles) handle.write(output);
        output.writeUTF(modData.getHomepage());
        output.writeUTF(modData.getDiscord());
        modData.getDownloadLink().write(output);
        output.writeInt(modData.getGid());
    }

    public static void setVersion(ModData modData, Version version) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 0);
            output.writeUTF(version == null ? null : version.toString());
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
            output.writeUTF(version == null ? null : version.toString());
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

    public static void setForceUpdateBeta(ModData modData, boolean forceUpdateBeta) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 12);
            output.writeBoolean(forceUpdateBeta);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setBetaMap(ModData modData, Version version, Boolean isBeta) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeUTF(modData.getName());
            output.writeByte((byte) 13);
            output.writeUTF(version.toString());
            output.writeByte(isBeta == null ? -1 : isBeta ? 1 : (byte) 0);
            changeModData(output.toByteArray());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    private static void changeModData(byte[] data) {
        try {
            HttpHeaders headers = new HttpHeaders();
            headers.add("token", TokenData.getTokens().get(0));
            HttpEntity<byte[]> entity = new HttpEntity<>(GZipFile.gzipData(data), headers);
            restTemplate.exchange(Settings.getInstance().getOtherLibURL() + "admin/modData", HttpMethod.PUT, entity, String.class);
        } catch (HttpStatusCodeException e) {
            logger.error(e.getMessage());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void setupModData() {
        try {
            HttpHeaders headers = new HttpHeaders();
            headers.add("token", TokenData.getTokens().get(0));
            HttpEntity<?> entity = new HttpEntity<>(headers);
            byte[] data = restTemplate.exchange(Settings.getInstance().getOtherLibURL() + "admin/modData", HttpMethod.GET, entity, byte[].class).getBody();
            ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
            int length = input.readInt();
            for(int i = 0; i < length; i++) ModData.createMod(input.readUTF(), input);
        } catch (HttpStatusCodeException e) {
            logger.error(e.getMessage());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void addRequestMods(long discordId, RawMod rawMod) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeLong(discordId);
            output.writeUTF(rawMod.mod.getName());
            output.writeUTF(rawMod.version.toString());
            HttpHeaders headers = new HttpHeaders();
            headers.add("token", TokenData.getTokens().get(0));
            HttpEntity<byte[]> entity = new HttpEntity<>(GZipFile.gzipData(output.toByteArray()), headers);
            restTemplate.exchange(Settings.getInstance().getOtherLibURL() + "admin/requestMods", HttpMethod.POST, entity, String.class);
        } catch (HttpStatusCodeException e) {
            logger.error(e.getMessage());
        } catch (Exception e) {
            logger.error(e);
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
            HttpHeaders headers = new HttpHeaders();
            headers.add("token", TokenData.getTokens().get(0));
            HttpEntity<byte[]> entity = new HttpEntity<>(GZipFile.gzipData(data), headers);
            restTemplate.exchange(Settings.getInstance().getOtherLibURL() + "admin/requestMods", HttpMethod.DELETE, entity, String.class);
        } catch (HttpStatusCodeException e) {
            logger.error(e.getMessage());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void changeRequestMods(long discordId, int i, RawMod rawMod) {
        try {
            @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
            output.writeLong(discordId);
            output.writeInt(i);
            output.writeUTF(rawMod.mod.getName());
            output.writeUTF(rawMod.version.toString());
            HttpHeaders headers = new HttpHeaders();
            headers.add("token", TokenData.getTokens().get(0));
            HttpEntity<byte[]> entity = new HttpEntity<>(GZipFile.gzipData(output.toByteArray()), headers);
            restTemplate.exchange(Settings.getInstance().getOtherLibURL() + "admin/requestMods", HttpMethod.PUT, entity, String.class);
        } catch (HttpStatusCodeException e) {
            logger.error(e.getMessage());
        } catch (Exception e) {
            logger.error(e);
        }
    }

    public static void loadModRequest() {
        try {
            HttpHeaders headers = new HttpHeaders();
            headers.add("token", TokenData.getTokens().get(0));
            HttpEntity<?> entity = new HttpEntity<>(headers);
            byte[] data = restTemplate.exchange(Settings.getInstance().getOtherLibURL() + "admin/requestMods", HttpMethod.GET, entity, byte[].class).getBody();
            ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
            int size = input.readInt();
            for(int i = 0; i < size; i++) {
                DiscordUserData userData = DiscordUserData.getUserData(input.readLong());
                int length = input.readInt();
                for(int j = 0; j < length; j++) {
                    String name = input.readUTF();
                    Version version = new Version(input.readUTF());
                    userData.addRequestMod(new RawMod(ModData.getModData(name), version));
                }
            }
        } catch (HttpStatusCodeException e) {
            logger.error(e.getMessage());
        } catch (Exception e) {
            logger.error(e);
        }
    }
}
