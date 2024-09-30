package kr.jongyeol.jaServer.controller;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import kr.jongyeol.jaServer.Compress;
import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.GZipFile;
import kr.jongyeol.jaServer.data.*;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.Cleanup;
import org.springframework.web.bind.annotation.*;

import java.util.Collection;

@RestController
@RequestMapping("/admin")
public class AdminController extends CustomController {
    @PutMapping("/modData")
    public String addModData(HttpServletResponse response, HttpServletRequest request,
                             @RequestBody byte[] data) throws Exception {
        if(checkPermission(response, request)) return null;
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
        String name = input.readUTF();
        info(request, "Add ModData: " + name);
        ModData modData = ModData.createMod(name, input);
        info(request, "Complete Add ModData: " + modData);
        return "Complete Add ModData";
    }

    @PatchMapping("/modData")
    public String changeModData(HttpServletResponse response, HttpServletRequest request,
                                @RequestBody byte[] data) throws Exception {
        if(checkPermission(response, request)) return null;
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
        ModData modData = ModData.getModData(input.readUTF());
        switch(input.readByte()) {
            case 0 -> {
                Version version = new Version(input.readUTF());
                modData.setVersion(version);
                info(request, modData.getName() + " version changed to " + version);
            }
            case 1 -> {
                Version version = new Version(input.readUTF());
                modData.setBetaVersion(version);
                info(request, modData.getName() + " betaVersion changed to " + version);
            }
            case 2 -> {
                boolean forceUpdate = input.readBoolean();
                modData.setForceUpdate(forceUpdate);
                info(request, modData.getName() + " forceUpdate changed to " + forceUpdate);
            }
            case 3 -> {
                ForceUpdateHandle[] handles = new ForceUpdateHandle[input.readInt()];
                for(int i = 0; i < handles.length; i++) handles[i] = new ForceUpdateHandle(input);
                modData.setForceUpdateHandles(handles);
                info(request, modData.getName() + " forceUpdateHandles changed");
            }
            case 4 -> {
                modData.addForceUpdateHandles(new ForceUpdateHandle(input));
                info(request, modData.getName() + " forceUpdateHandles added");
            }
            case 5 -> {
                int i = input.readInt();
                modData.removeForceUpdateHandles(i);
                info(request, modData.getName() + " forceUpdateHandles removed at " + i);
            }
            case 6 -> {
                int i = input.readInt();
                modData.changeForceUpdateHandles(i, new ForceUpdateHandle(input));
                info(request, modData.getName() + " forceUpdateHandles changed at " + i);
            }
            case 7 -> {
                String homepage = input.readUTF();
                modData.setHomepage(homepage);
                info(request, modData.getName() + " homepage changed to " + homepage);
            }
            case 8 -> {
                modData.setDownloadLink(DownloadLink.createDownloadLink(modData, input));
                info(request, modData.getName() + " downloadLink changed");
            }
            case 9 -> {
                int gid = input.readInt();
                modData.setGid(gid);
                info(request, modData.getName() + " gid changed to " + gid);
            }
            case 10 -> {
                modData.loadLocalizations();
                info(request, modData.getName() + " localizations loaded");
            }
            case 11 -> {
                String discord = input.readUTF();
                modData.setDiscord(discord);
                info(request, modData.getName() + " discord changed to " + discord);
            }
        }
        return "Complete Change ModData";
    }

    @GetMapping("/modData")
    public byte[] getModData(HttpServletResponse response, HttpServletRequest request) {
        if(checkPermission(response, request)) return null;
        info(request, "Get ModData");
        ModData[] modDatas = ModData.getModDataList();
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeInt(modDatas.length);
        for(ModData modData : modDatas) {
            output.writeUTF(modData.getName());
            output.writeUTF(modData.getVersion().toString());
            output.writeUTF(modData.getBetaVersion().toString());
            output.writeBoolean(modData.isForceUpdate());
            ForceUpdateHandle[] handles = modData.getForceUpdateHandles();
            output.writeInt(handles.length);
            for(ForceUpdateHandle handle : handles) handle.write(output);
            output.writeUTF(modData.getHomepage());
            output.writeUTF(modData.getDiscord());
            modData.getDownloadLink().write(output);
            output.writeInt(modData.getGid());
        }
        return GZipFile.gzipData(output.toByteArray());
    }

    @PutMapping("/requestMods")
    public String addRequestMods(HttpServletResponse response, HttpServletRequest request,
                                 @RequestBody byte[] data) throws Exception {
        if(checkPermission(response, request)) return null;
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
        long discordID = input.readLong();
        DiscordUserData userData = DiscordUserData.getUserData(discordID);
        RawMod mod = new RawMod(ModData.getModData(input.readUTF()), new Version(input.readUTF()));
        userData.addRequestMod(mod);
        Connection.connections.stream().filter(c -> c.connectInfo.steamID == userData.steamID).forEach(Connection::loadModRequest);
        info(request, "RequestMods Added: " + discordID + "(steam:" + userData.steamID + ") " + mod.mod.getName() + " " + mod.version);
        return "Complete Add RequestMods";
    }

    @DeleteMapping("/requestMods")
    public String removeRequestMods(HttpServletResponse response, HttpServletRequest request,
                                    @RequestBody byte[] data) throws Exception {
        if(checkPermission(response, request)) return null;
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
        long discordID = input.readLong();
        DiscordUserData userData = DiscordUserData.getUserData(discordID);
        if(input.readBoolean()) {
            userData.resetRequestMods();
            info(request, "RequestMods Reset: " + discordID + "(steam:" + userData.steamID + ") ");
        } else {
            int i = input.readInt();
            userData.removeRequestMod(i);
            info(request, "RequestMods Removed: " + discordID + "(steam:" + userData.steamID + ") " + i);
        }
        return "Complete Remove RequestMods";
    }

    @PatchMapping("/requestMods")
    public String changeRequestMods(HttpServletResponse response, HttpServletRequest request,
                                 @RequestBody byte[] data) throws Exception {
        if(checkPermission(response, request)) return null;
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
        long discordID = input.readLong();
        DiscordUserData userData = DiscordUserData.getUserData(discordID);
        int i = input.readInt();
        RawMod mod = new RawMod(ModData.getModData(input.readUTF()), new Version(input.readUTF()));
        userData.changeRequestMod(i, mod);
        info(request, "RequestMods Changed: " + discordID + "(steam:" + userData.steamID + ") " + i + " " + mod.mod.getName() + " " + mod.version);
        return "Complete Change RequestMods";
    }

    @GetMapping("/requestMods")
    public byte[] getRequestMods(HttpServletResponse response, HttpServletRequest request) {
        if(checkPermission(response, request)) return null;
        info(request, "Get RequestMods");
        Collection<DiscordUserData> userDatas = DiscordUserData.getUserData();
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeInt(userDatas.size());
        for(DiscordUserData userData : userDatas) {
            output.writeLong(userData.steamID);
            RawMod[] requestMods = userData.getRequestMods();
            output.writeInt(requestMods.length);
            for(RawMod mod : requestMods) {
                output.writeUTF(mod.mod.getName());
                output.writeUTF(mod.version.toString());
            }
        }
        return GZipFile.gzipData(output.toByteArray());
    }
}
