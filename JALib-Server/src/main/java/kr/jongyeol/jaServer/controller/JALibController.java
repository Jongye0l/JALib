package kr.jongyeol.jaServer.controller;

import jakarta.servlet.http.HttpServletRequest;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.data.Language;
import kr.jongyeol.jaServer.data.ModData;
import kr.jongyeol.jaServer.data.TokenData;
import kr.jongyeol.jaServer.data.Version;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.Cleanup;
import org.springframework.core.io.FileSystemResource;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.nio.file.Path;

@RestController
public class JALibController extends CustomController {
    @PostMapping("/exceptionInfo")
    public void exceptionInfo() {

    }

    @GetMapping("/modInfo/{name}/{version}/{beta}")
    public byte[] modInfo(HttpServletRequest request, @PathVariable String name, @PathVariable String version, @PathVariable int beta) {
        info(request, "GetModInfo: " + name + " " + version + ", beta: " + (beta == 1));
        ModData modData = ModData.getModData(name);
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        Version ver = modData == null ? null : beta == 1 ? modData.getBetaVersion() : modData.getVersion();
        output.writeBoolean(ver != null);
        if(ver == null) return output.toByteArray();
        output.writeUTF(ver.toString());
        output.writeBoolean((beta != 0 ? modData.isForceUpdateBeta() : modData.isForceUpdate()) || modData.checkForceUpdate(new Version(version)));
        Language[] languages = modData.getAvailableLanguages();
        output.writeByte((byte) languages.length);
        for(Language language : languages) output.writeByte((byte) language.ordinal());
        output.writeUTF(modData.getHomepage());
        output.writeUTF(modData.getDiscord());
        output.writeInt(modData.getGid());
        return output.toByteArray();
    }

    @GetMapping("/modInfoV2/{name}/{version}")
    public byte[] modInfoV2(HttpServletRequest request, @PathVariable String name, @PathVariable String version) {
        info(request, "GetModInfo(V2): " + name + " " + version + ", beta: null");
        ModData modData = ModData.getModData(name);
        Version curVer = version.toLowerCase().equals("latest") ? null : new Version(version);
        if(curVer == null) {
            curVer = modData.getVersion();
            if(curVer == null) curVer = modData.getBetaVersion();
        }
        boolean beta = modData != null && modData.getBetaMap().containsKey(curVer);
        return modInfoV2(modData, curVer, beta);
    }

    @GetMapping("/modInfoV2/{name}/{version}/{beta}")
    public byte[] modInfoV2(HttpServletRequest request, @PathVariable String name, @PathVariable String version, @PathVariable int beta) {
        info(request, "GetModInfo(V2): " + name + " " + version + ", beta: " + (beta == 1));
        ModData modData = ModData.getModData(name);
        return modInfoV2(modData, version.toLowerCase().equals("latest") ? beta == 0 ? modData.getVersion() : modData.getBetaVersion() : new Version(version), beta != 0);
    }

    private byte[] modInfoV2(ModData modData, Version version, boolean beta) {
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        boolean success = modData != null && modData.getBetaVersion() != null;
        output.writeBoolean(success);
        if(!success) return output.toByteArray();
        Version ver = modData.getVersion();
        output.writeUTF(ver == null ? null : ver.toString());
        output.writeUTF(modData.getBetaVersion().toString());
        output.writeBoolean(modData.isForceUpdate());
        output.writeBoolean(modData.isForceUpdateBeta());
        boolean needUpdate = version.isUpper(beta ? modData.getBetaVersion() : modData.getVersion())
            && ((beta ? modData.isForceUpdateBeta() : modData.isForceUpdate()) || modData.checkForceUpdate(version));
        output.writeUTF(needUpdate ?
            (beta ? modData.getBetaVersion() : modData.getVersion()).toString()
            : null);
        Language[] languages = modData.getAvailableLanguages();
        output.writeByte((byte) languages.length);
        for(Language language : languages) output.writeByte((byte) language.ordinal());
        output.writeUTF(modData.getHomepage());
        output.writeUTF(modData.getDiscord());
        output.writeInt(modData.getGid());
        return output.toByteArray();
    }

    @GetMapping("/downloadMod/{name}/{version}")
    public ResponseEntity<?> downloadMod(HttpServletRequest request, @PathVariable String name, @PathVariable String version) {
        try {
            info(request, "DownloadMod: " + name + " " + version);
            File modFile = Path.of(Settings.getInstance().getModFilePath(), name, version).toFile();
            if(modFile.exists()) {
                FileSystemResource resource = new FileSystemResource(modFile);
                HttpHeaders headers = new HttpHeaders();
                headers.add(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=" + name + ".zip");
                return ResponseEntity.ok()
                    .headers(headers)
                    .body(resource);
            }
            ModData modData = ModData.getModData(name);
            Version ver = version.toLowerCase().equals("latest") ? null : new Version(version);
            if(ver == null) {
                ver = modData.getVersion();
                if(ver == null) ver = modData.getBetaVersion();
            }
            return ResponseEntity.status(HttpStatus.FOUND)
                .location(URI.create(modData.getDownloadLink().getLink(ver)))
                .build();
        } catch (Exception e) {
            error(request, e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("Internal Server Error");
        }
    }

    @GetMapping("/ping")
    public String ping(HttpServletRequest request) {
        info(request, "ping!");
        return "pong!";
    }

    @GetMapping("/reloadToken")
    public String reloadToken(HttpServletRequest request) throws IOException {
        info(request, "reloading Token..");
        TokenData.LoadToken();
        return "Token reloaded!";
    }

    @GetMapping("/autoInstaller/{version}")
    public ResponseEntity<?> autoInstaller(HttpServletRequest request, @PathVariable String version) {
        info(request, "GetAutoInstaller: " + version);
        Version ver = new Version(version);
        ModData modData = ModData.getModData("JALib");
        Version version1 = modData.getBetaVersion();
        if(ver.isUpper(version1))
            return downloadMod(request, "JALib", modData.getBetaVersion().toString());
        return ResponseEntity.status(HttpStatus.NOT_MODIFIED).build();
    }
}
