package kr.jongyeol.jaServer.data;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.Variables;
import lombok.*;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Data
public class ModData {
    private static List<ModData> modDataList = new ArrayList<>();
    public static Class<? extends ModData> clazz;
    private String name;
    private Version version;
    private Version betaVersion;
    private boolean forceUpdate;
    private ForceUpdateHandle[] forceUpdateHandles = new ForceUpdateHandle[0];
    private Language[] availableLanguages = new Language[0];
    private String homepage;
    private String discord;
    private DownloadLink downloadLink;
    private int gid;
    private Map<String, Map<Language, String>> localizations = new HashMap<>();
    @Getter(AccessLevel.NONE) @Setter(AccessLevel.NONE)
    private final transient Object forceUpdateLocker = new Object();

    public ModData() {
        modDataList.add(this);
    }

    public static ModData getModData(String name) {
        for(ModData modData : modDataList) if(modData.getName().equals(name)) return modData;
        return null;
    }

    public static void LoadModData(Class<? extends ModData> cl) throws IOException {
        clazz = cl;
        File folder = new File(Settings.instance.modDataPath);
        for(File file : folder.listFiles()) Variables.gson.fromJson(Files.readString(file.toPath()), cl);
    }

    public static ModData[] getModDataList() {
        return modDataList.toArray(new ModData[0]);
    }

    public void save() throws IOException {
        Path path = Path.of(Settings.instance.modDataPath, name);
        String json = Variables.gson.toJson(this);
        Files.writeString(path, json);
    }

    public void setName(String name) throws IOException {
        this.name = name;
        save();
    }

    public void setVersion(Version version) throws IOException {
        this.version = version;
        save();
    }

    public void setBetaVersion(Version betaVersion) throws IOException {
        this.betaVersion = betaVersion;
        save();
    }

    public void setForceUpdateHandles(ForceUpdateHandle[] forceUpdateHandles) throws IOException {
        synchronized(forceUpdateLocker) {
            this.forceUpdateHandles = forceUpdateHandles;
        }
        save();
    }

    public void addForceUpdateHandles(ForceUpdateHandle forceUpdateHandle) throws IOException {
        synchronized(forceUpdateLocker) {
            ForceUpdateHandle[] newHandles = new ForceUpdateHandle[forceUpdateHandles.length + 1];
            System.arraycopy(forceUpdateHandles, 0, newHandles, 0, forceUpdateHandles.length);
            newHandles[forceUpdateHandles.length] = forceUpdateHandle;
            forceUpdateHandles = newHandles;
        }
        save();
    }

    public void removeForceUpdateHandles(int i) throws IOException {
        synchronized(forceUpdateLocker) {
            ForceUpdateHandle[] newHandles = new ForceUpdateHandle[forceUpdateHandles.length - 1];
            System.arraycopy(forceUpdateHandles, 0, newHandles, 0, i);
            System.arraycopy(forceUpdateHandles, i + 1, newHandles, i, forceUpdateHandles.length - i - 1);
            forceUpdateHandles = newHandles;
        }
        save();
    }

    public void changeForceUpdateHandles(int i, ForceUpdateHandle forceUpdateHandle) throws IOException {
        synchronized(forceUpdateLocker) {
            forceUpdateHandles[i] = forceUpdateHandle;
        }
        save();
    }

    public boolean checkForceUpdate(Version version) {
        synchronized(forceUpdateLocker) {
            for(ForceUpdateHandle handle : forceUpdateHandles)
                if(!version.isUpper(handle.version1) && !handle.version2.isUpper(version)) return handle.forceUpdate;
        }
        return forceUpdate;
    }

    public void setForceUpdate(boolean forceUpdate) throws IOException {
        this.forceUpdate = forceUpdate;
        save();
    }

    public void setGid(int gid) throws IOException {
        this.gid = gid;
        save();
    }

    public void loadLocalizations() throws IOException {
        URL url = new URL("https://docs.google.com/spreadsheets/d/1kx12GMqK9lgpiZimBSAMdj51xY4IuQUSLXzmQFZ6Sk4/gviz/tq?tqx=out:json&tq&gid=" + gid);
        @Cleanup InputStream stream = url.openStream();
        String json = new String(stream.readAllBytes());
        json = json.substring(json.indexOf("{"), json.lastIndexOf("}") + 1);
        JsonArray array = JsonParser.parseString(json).getAsJsonObject().getAsJsonObject("table").getAsJsonArray("rows");
        Map<String, Map<Language, String>> newLocalizations = new HashMap<>();
        List<Language> languages = new ArrayList<>();
        for(JsonElement element : array.get(0).getAsJsonObject().getAsJsonArray("c")) {
            if(element.isJsonNull()) break;
            JsonElement element1 = element.getAsJsonObject().get("v");
            if(element1.isJsonNull()) break;
            String string = element1.getAsString();
            if(string.equals("Language")) continue;
            languages.add(Language.valueOf(string));
        }
        for(int i = 1; i < array.size(); i++) {
            JsonObject object = array.get(i).getAsJsonObject();
            String key = object.getAsJsonArray("c").get(0).getAsJsonObject().get("v").getAsString();
            Map<Language, String> map = new HashMap<>();
            for(int j = 1; j < object.getAsJsonArray("c").size(); j++) {
                JsonElement element = object.getAsJsonArray("c").get(j).getAsJsonObject().get("v");
                if(element.isJsonNull()) continue;
                map.put(languages.get(j - 1), element.getAsString());
            }
            newLocalizations.put(key, map);
        }
        localizations = newLocalizations;
        availableLanguages = languages.toArray(new Language[0]);
        save();
    }

    public void setHomepage(String homepage) throws IOException {
        this.homepage = homepage;
        save();
    }

    public void setDiscord(String discord) throws IOException {
        this.discord = discord;
        save();
    }
}
