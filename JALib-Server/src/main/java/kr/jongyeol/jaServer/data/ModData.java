package kr.jongyeol.jaServer.data;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonParser;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.Variables;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
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
    private static Map<String, ModData> modDataList = new HashMap<>();
    public static Class<? extends ModData> clazz = ModData.class;
    private String name;
    private Version version;
    private Version betaVersion;
    private boolean forceUpdate;
    private boolean forceUpdateBeta = true;
    private Map<Version, Boolean> betaMap = new HashMap<>();
    private ForceUpdateHandle[] forceUpdateHandles = new ForceUpdateHandle[0];
    private Language[] availableLanguages = new Language[0];
    private String homepage;
    private String discord;
    private DownloadLink downloadLink;
    private int gid;
    private Map<String, Map<Language, String>> localizations = new HashMap<>();
    @Getter(AccessLevel.NONE) @Setter(AccessLevel.NONE)
    private final transient Object forceUpdateLocker = new Object();

    public static ModData getModData(String name) {
        return modDataList.get(name);
    }

    public static void LoadModData() throws IOException {
        File folder = new File(Settings.getInstance().getModDataPath());
        for(File file : folder.listFiles()) {
            Path path = file.toPath();
            if(path.endsWith(".old") && Files.exists(Path.of(path.toString().replace(".old", "")))) continue;
            ModData modData = Variables.gson.fromJson(Files.readString(path), clazz);
            modDataList.put(modData.name, modData);
        }
    }

    public static ModData createMod() throws Exception {
        return ModData.clazz.getConstructor().newInstance();
    }

    public static ModData createMod(String name, ByteArrayDataInput input) throws Exception {
        ModData modData = ModData.getModData(name);
        if(modData == null) {
            modData = ModData.createMod();
            modDataList.put(name, modData);
        }
        modData.name = name;
        String versionString = input.readUTF();
        modData.version = versionString == null ? null : new Version(versionString);
        versionString = input.readUTF();
        modData.betaVersion = versionString == null ? null : new Version(versionString);
        modData.forceUpdate = input.readBoolean();
        modData.forceUpdateBeta = input.readBoolean();
        Map<Version, Boolean> betaMap = new HashMap<>();
        int size = input.readInt();
        for(int i = 0; i < size; i++) betaMap.put(new Version(input.readUTF()), input.readBoolean());
        modData.betaMap = betaMap;
        ForceUpdateHandle[] handles = new ForceUpdateHandle[input.readInt()];
        for(int i = 0; i < handles.length; i++) handles[i] = new ForceUpdateHandle(input);
        modData.forceUpdateHandles = handles;
        modData.homepage = input.readUTF();
        modData.discord = input.readUTF();
        modData.downloadLink = DownloadLink.createDownloadLink(modData, input);
        modData.gid = input.readInt();
        modData.save();
        return modData;
    }

    public static ModData[] getModDataList() {
        return modDataList.values().toArray(new ModData[0]);
    }

    public static String[] getModNames() {
        return modDataList.keySet().toArray(new String[0]);
    }

    public void save() throws IOException {
        String modDataPath = Settings.getInstance().getModDataPath();
        Path path = Path.of(modDataPath, name);
        boolean exists = Files.exists(path);
        Path copyPath = null;
        if(exists) {
            copyPath = Path.of(modDataPath, name + ".old");
            Files.move(path, copyPath);
        }
        String json = Variables.gson.toJson(this, clazz);
        if(!Files.exists(path)) Files.createFile(path);
        Files.writeString(path, json);
        if(exists) Files.delete(copyPath);
    }

    public void setName(String name) throws IOException {
        this.name = name;
        save();
    }

    public void setVersion(Version version) throws IOException {
        this.version = version;
        this.betaMap.put(version, false);
        save();
    }

    public void setBetaVersion(Version betaVersion) throws IOException {
        this.betaVersion = betaVersion;
        this.betaMap.put(betaVersion, true);
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

    public void setForceUpdateBeta(boolean forceUpdateBeta) throws IOException {
        this.forceUpdateBeta = forceUpdateBeta;
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
        //Map<String, Map<Language, String>> newLocalizations = new HashMap<>();
        List<Language> languages = new ArrayList<>();
        for(JsonElement element : array.get(0).getAsJsonObject().getAsJsonArray("c")) {
            if(element.isJsonNull()) break;
            JsonElement element1 = element.getAsJsonObject().get("v");
            if(element1.isJsonNull()) break;
            String string = element1.getAsString();
            if(string.equals("Language")) continue;
            languages.add(Language.valueOf(string));
        }
        //for(int i = 1; i < array.size(); i++) {
        //    JsonObject object = array.get(i).getAsJsonObject();
        //    String key = object.getAsJsonArray("c").get(0).getAsJsonObject().get("v").getAsString();
        //    Map<Language, String> map = new HashMap<>();
        //    for(int j = 1; j < object.getAsJsonArray("c").size(); j++) {
        //        JsonElement element = object.getAsJsonArray("c").get(j).getAsJsonObject().get("v");
        //        if(element.isJsonNull()) continue;
        //        map.put(languages.get(j - 1), element.getAsString());
        //    }
        //    newLocalizations.put(key, map);
        //}
        //localizations = newLocalizations;
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

    public void setDownloadLink(DownloadLink downloadLink) throws IOException {
        this.downloadLink = downloadLink;
        save();
    }
}
