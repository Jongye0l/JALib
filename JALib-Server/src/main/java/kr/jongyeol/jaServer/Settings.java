package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.data.AutoRemovedData;
import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.Setter;
import lombok.SneakyThrows;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

@EqualsAndHashCode(callSuper = false)
@Data
public class Settings extends AutoRemovedData {
    private static Settings instance;
    public static Class<? extends Settings> clazz = Settings.class;
    private String logPath;
    private String userDataPath;
    private String discordUserDataPath;
    private String modDataPath;
    private String adminManagerPath;
    private String tokenPath;
    private String otherLibURL;
    private String modFilePath;

    @SneakyThrows(IOException.class)
    public static Settings getInstance() {
        if(instance == null) instance = Variables.gson.fromJson(Files.readString(Path.of("settings.json")), clazz);
        else instance.use();
        return instance;
    }

    public String getLogPath() {
        use();
        return logPath;
    }

    public String getUserDataPath() {
        use();
        return userDataPath;
    }

    public String getDiscordUserDataPath() {
        use();
        return discordUserDataPath;
    }

    public String getModDataPath() {
        use();
        return modDataPath;
    }

    public String getAdminManagerPath() {
        use();
        return adminManagerPath;
    }

    public String getTokenPath() {
        use();
        return tokenPath;
    }

    public String getOtherLibURL() {
        use();
        return otherLibURL;
    }

    public String getModFilePath() {
        use();
        return modFilePath;
    }
}
