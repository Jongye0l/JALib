package kr.jongyeol.jaServer.data;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
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
import java.util.*;

@EqualsAndHashCode(callSuper = false)
@Data
public class ModData extends AutoRemovedData {
    private static Map<String, ModData> modDataList = new HashMap<>();
    public static Class<? extends ModData> clazz = ModData.class;
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
    @Setter(AccessLevel.NONE)
    private final transient Object forceUpdateLocker = new Object();

    public ModData() {
        modDataList.put(name, this);
    }

    @SneakyThrows(IOException.class)
    public static ModData getModData(String name) {
        if(modDataList.containsKey(name)) {
            ModData modData = modDataList.get(name);
            modData.use();
            return modData;
        }
        Path path = Path.of(Settings.getInstance().getModDataPath(), name);
        if(!Files.exists(path)) {
            path = Path.of(Settings.getInstance().getModDataPath(), name + ".old");
            if(!Files.exists(path)) return null;
        }
        return Variables.gson.fromJson(Files.readString(path), clazz);
    }

    public static ModData createMod() throws Exception {
        return ModData.clazz.getConstructor().newInstance();
    }

    public static ModData createMod(String name, ByteArrayDataInput input) throws Exception {
        ModData modData = ModData.getModData(name);
        if(modData == null) modData = ModData.createMod();
        modData.name = name;
        modData.version = new Version(input.readUTF());
        modData.betaVersion = new Version(input.readUTF());
        modData.forceUpdate = input.readBoolean();
        ForceUpdateHandle[] handles = new ForceUpdateHandle[input.readInt()];
        for(int j = 0; j < handles.length; j++) handles[j] = new ForceUpdateHandle(input);
        modData.forceUpdateHandles = handles;
        modData.homepage = input.readUTF();
        modData.discord = input.readUTF();
        modData.downloadLink = DownloadLink.createDownloadLink(modData, input);
        modData.gid = input.readInt();
        modData.save();
        return modData;
    }

    public static ModData[] getModDataList() {
        List<String> getModNames = getModNames();
        ModData[] modData = new ModData[getModNames.size()];
        for(int i = 0; i < getModNames.size(); i++) modData[i] = getModData(getModNames.get(i));
        return modData;
    }

    public static List<String> getModNames() {
        List<String> names = new ArrayList<>();
        File folder = new File(Settings.getInstance().getModDataPath());
        for(File file : folder.listFiles()) {
            String name = file.getName();
            if(name.endsWith(".old")) name = name.substring(0, name.length() - 4);
            if(!names.contains(name)) names.add(name);
        }
        return names;
    }

    public static Enumeration<ModData> getMods() {
        List<String> getModNames = getModNames();
        return new Enumeration<>() {
            int i = 0;

            @Override
            public boolean hasMoreElements() {
                return i < getModNames.size();
            }

            @Override
            public ModData nextElement() {
                return getModData(getModNames.get(i++));
            }
        };
    }

    public void save() throws IOException {
        use();
        String modDataPath = Settings.getInstance().getModDataPath();
        Path path = Path.of(modDataPath, name);
        boolean exists = Files.exists(path);
        Path copyPath = null;
        if(exists) {
            copyPath = Path.of(modDataPath, name + ".old");
            Files.move(path, copyPath);
        }
        String json = Variables.gson.toJson(this);
        Files.writeString(path, json);
        if(exists) Files.delete(copyPath);
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
        use();
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
        use();
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

    public void setDownloadLink(DownloadLink downloadLink) throws IOException {
        this.downloadLink = downloadLink;
        save();
    }

    @Override
    public void onRemove() {
        modDataList.remove(name, this);
    }

    public String getName() {
        use();
        return name;
    }

    public Version getVersion() {
        use();
        return version;
    }

    public Version getBetaVersion() {
        use();
        return betaVersion;
    }

    public Language[] getAvailableLanguages() {
        use();
        return availableLanguages;
    }

    public String getHomepage() {
        use();
        return homepage;
    }

    public String getDiscord() {
        use();
        return discord;
    }

    public DownloadLink getDownloadLink() {
        use();
        return downloadLink;
    }

    public int getGid() {
        use();
        return gid;
    }

    public Map<String, Map<Language, String>> getLocalizations() {
        use();
        return localizations;
    }
}
