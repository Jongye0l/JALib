package kr.jongyeol.jaServer;

import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.SneakyThrows;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

@EqualsAndHashCode(callSuper = false)
@Data
public class Settings {
    private static Settings instance;
    public static Class<? extends Settings> clazz = Settings.class;
    private String logPath;
    private String modDataPath;
    private String adminManagerPath;
    private String tokenPath;
    private String otherLibURL;
    private String modFilePath;

    @SneakyThrows(IOException.class)
    public static Settings getInstance() {
        if(instance == null) instance = Variables.gson.fromJson(Files.readString(Path.of("settings.json")), clazz);
        return instance;
    }
}
