package kr.jongyeol.jaServer.data;

import com.google.gson.reflect.TypeToken;
import kr.jongyeol.jaServer.Logger;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.Variables;
import lombok.*;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Collection;
import java.util.Map;

@Data
public class DiscordUserData {
    private static Map<Long, DiscordUserData> userDataMap;
    private static AutoRemovedData autoRemovedData;

    @SneakyThrows(IOException.class)
    public static void checkLoad() {
        if(userDataMap != null) {
            autoRemovedData.use();
            return;
        }
        userDataMap = Variables.gson.fromJson(Files.readString(Path.of(Settings.instance.discordUserDataPath)), new TypeToken<Map<Long, DiscordUserData>>() {}.getType());
        autoRemovedData = new AutoRemovedData() {
            @Override
            public void onRemove() {
                try {
                    save();
                    for(DiscordUserData userData : userDataMap.values()) {
                        Variables.setNull(userData.requestMods);
                        Variables.setNull(userData);
                    }
                    userDataMap.clear();
                    userDataMap = null;
                    autoRemovedData = null;
                } catch (IOException e) {
                    Logger.MAIN_LOGGER.error(e);
                }
            }
        };
    }

    public long steamID;
    public RawMod[] requestMods = new RawMod[0];
    public transient boolean saveRequest = true;
    @Getter(AccessLevel.NONE) @Setter(AccessLevel.NONE)
    private final transient Object requestLocker = new Object();

    public static void save() throws IOException {
        Files.writeString(Path.of(Settings.instance.discordUserDataPath), Variables.gson.toJson(userDataMap));
    }

    public static DiscordUserData getUserData(long id) {
        checkLoad();
        return userDataMap.computeIfAbsent(id, k -> new DiscordUserData());
    }

    public static Collection<DiscordUserData> getUserData() {
        checkLoad();
        return userDataMap.values();
    }

    public static boolean hasUserData(long id) {
        checkLoad();
        return userDataMap.containsKey(id);
    }

    public void addRequestMod(RawMod mod) throws IOException {
        autoRemovedData.use();
        if(hasRequestMod(mod)) return;
        synchronized(requestLocker) {
            RawMod[] newRequestMods = new RawMod[requestMods.length + 1];
            System.arraycopy(requestMods, 0, newRequestMods, 0, requestMods.length);
            newRequestMods[requestMods.length] = mod;
            requestMods = newRequestMods;
        }
        if(saveRequest) save();
    }

    public void removeRequestMod(int i) throws IOException {
        autoRemovedData.use();
        synchronized(requestLocker) {
            RawMod[] newRequestMods = new RawMod[requestMods.length - 1];
            System.arraycopy(requestMods, 0, newRequestMods, 0, i);
            System.arraycopy(requestMods, i + 1, newRequestMods, i, requestMods.length - i - 1);
            requestMods = newRequestMods;
        }
        if(saveRequest) save();
    }

    public void changeRequestMod(int i, RawMod mod) throws IOException {
        autoRemovedData.use();
        synchronized(requestLocker) {
            requestMods[i] = mod;
        }
        if(saveRequest) save();
    }

    public boolean hasRequestMod(RawMod mod) {
        autoRemovedData.use();
        for(RawMod requestMod : requestMods) if(requestMod.equals(mod)) return true;
        return false;
    }

    public RawMod[] getAndRemoveRequestMods() throws IOException {
        autoRemovedData.use();
        synchronized(requestLocker) {
            RawMod[] requestMods = this.requestMods;
            this.requestMods = new RawMod[0];
            if(saveRequest) save();
            return requestMods;
        }
    }

    public void resetRequestMods() throws IOException {
        autoRemovedData.use();
        synchronized(requestLocker) {
            requestMods = new RawMod[0];
            if(saveRequest) save();
        }
    }
}
