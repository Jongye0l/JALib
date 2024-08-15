package kr.jongyeol.jaServer;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

public class Settings {
    public static Settings instance;
    public String logPath;
    public String userDataPath;
    public String discordUserDataPath;
    public String modDataPath;
    public String adminManagerPath;
    public String tokenPath;
    public String otherLibURL;

    public static void load(Class<? extends Settings> cl) throws IOException {
        instance = Variables.gson.fromJson(Files.readString(Path.of("settings.json")), cl);
    }
}
