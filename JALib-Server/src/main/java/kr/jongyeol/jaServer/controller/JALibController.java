package kr.jongyeol.jaServer.controller;

import jakarta.servlet.http.HttpServletRequest;
import kr.jongyeol.jaServer.data.Language;
import kr.jongyeol.jaServer.data.ModData;
import kr.jongyeol.jaServer.data.TokenData;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import lombok.Cleanup;
import org.springframework.web.bind.annotation.*;

import java.io.IOException;
import java.util.Arrays;
import java.util.Map;

@RestController
public class JALibController extends CustomController {
    @PostMapping("/exceptionInfo")
    public void exceptionInfo() {

    }

    @GetMapping("/localization/{name}/{langInt}")
    public byte[] getLocalization(HttpServletRequest request, @PathVariable String name, @PathVariable int langInt) {
        Language language = Language.values()[langInt];
        info(request, "GetLocalization: " + name + " " + language);
        ModData modData = ModData.getModData(name);
        if(modData == null) return new byte[0];
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        Language finalLanguage = language;
        if(Arrays.stream(modData.getAvailableLanguages()).noneMatch(l -> l == finalLanguage))
            language = Arrays.stream(modData.getAvailableLanguages()).anyMatch(l -> l == Language.English) ? Language.English : modData.getAvailableLanguages()[0];
        output.writeByte((byte) language.ordinal());
        Map<String, Map<Language, String>> localization = modData.getLocalizations();
        output.writeInt(localization.size());
        for(Map.Entry<String, Map<Language, String>> entry : localization.entrySet()) {
            output.writeUTF(entry.getKey());
            if(entry.getValue().containsKey(language)) output.writeUTF(entry.getValue().get(language));
            else if(entry.getValue().containsKey(Language.English)) output.writeUTF(entry.getValue().get(Language.English));
            else output.writeUTF(entry.getValue().values().stream().findFirst().get());
        }
        return output.toByteArray();
    }

    @GetMapping("/modInfo/{name}/{version}/{beta}")
    public byte[] modInfo(HttpServletRequest request, @PathVariable String name, @PathVariable String version, @PathVariable int beta) {
        info(request, "GetModInfo: " + name + " " + version + ", beta: " + (beta == 1));
        ModData modData = ModData.getModData(name);
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        output.writeBoolean(modData != null);
        if(modData == null) return output.toByteArray();
        output.writeUTF((beta == 1 ? modData.getBetaVersion() : modData.getVersion()).toString());
        output.writeBoolean(modData.isForceUpdate());
        Language[] languages = modData.getAvailableLanguages();
        output.writeByte((byte) languages.length);
        for(Language language : languages) output.writeByte((byte) language.ordinal());
        String homepage = modData.getHomepage();
        output.writeBoolean(homepage != null);
        if(homepage != null) output.writeUTF(homepage);
        String discord = modData.getDiscord();
        output.writeBoolean(discord != null);
        if(discord != null) output.writeUTF(discord);
        return output.toByteArray();
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
}
