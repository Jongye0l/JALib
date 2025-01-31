package kr.jongyeol.jaServer.controller;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import kr.jongyeol.jaServer.ConnectOtherLib;
import kr.jongyeol.jaServer.GZipFile;
import kr.jongyeol.jaServer.data.*;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.Cleanup;
import org.springframework.web.bind.annotation.*;

import java.util.Arrays;
import java.util.Map;

@RestController
@RequestMapping("/admin")
public class AdminController extends CustomController {
    @PostMapping("/modData")
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

    @PutMapping("/modData")
    public String changeModData(HttpServletResponse response, HttpServletRequest request,
                                @RequestBody byte[] data) throws Exception {
        if(checkPermission(response, request)) return null;
        @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(data));
        ModData modData = ModData.getModData(input.readUTF());
        switch(input.readByte()) {
            case 0 -> {
                String versionStr = input.readUTF();
                Version version = versionStr == null ? null : new Version(versionStr);
                modData.setVersion(version);
                info(request, modData.getName() + " version changed to " + version);
            }
            case 1 -> {
                String versionStr = input.readUTF();
                Version version = versionStr == null ? null : new Version(versionStr);
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
            case 12 -> {
                boolean forceUpdate = input.readBoolean();
                modData.setForceUpdateBeta(forceUpdate);
                info(request, modData.getName() + " forceUpdateBeta changed to " + forceUpdate);
            }
            case 13 -> {
                Version version = new Version(input.readUTF());
                byte isBeta = input.readByte();
                Map<Version, Boolean> betaMap = modData.getBetaMap();
                if(isBeta == -1) betaMap.remove(version);
                else betaMap.put(version, isBeta == 1);
                info(request, modData.getName() + " betaMap changed");
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
        ModData[] filtered = Arrays.stream(modDatas).toArray(ModData[]::new);
        output.writeInt(filtered.length);
        for(ModData modData : filtered) ConnectOtherLib.modToBytes(output, modData);
        return GZipFile.gzipData(output.toByteArray());
    }
}
