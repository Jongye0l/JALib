package kr.jongyeol.jaServer.data;

import com.google.gson.reflect.TypeToken;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.Variables;
import lombok.AccessLevel;
import lombok.Data;
import lombok.Getter;
import lombok.Setter;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Map;

@Data
public class DiscordUserData {
    private static Map<Long, DiscordUserData> userDataMap;

    static {
        try {
            userDataMap = Variables.gson.fromJson(Files.readString(Path.of(Settings.instance.discordUserDataPath)), new TypeToken<Map<Long, DiscordUserData>>() {}.getType());
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public long steamID;
    public RawMod[] requestMods = new RawMod[0];
    @Getter(AccessLevel.NONE) @Setter(AccessLevel.NONE)
    private final transient Object requestLocker = new Object();

    public static void save() throws IOException {
        Files.writeString(Path.of(Settings.instance.discordUserDataPath), Variables.gson.toJson(userDataMap));
    }

    public static DiscordUserData getUserData(long id) {
        return userDataMap.computeIfAbsent(id, k -> new DiscordUserData());
    }

    public static boolean hasUserData(long id) {
        return userDataMap.containsKey(id);
    }

    public void addRequestMod(RawMod mod) throws IOException {
        if(hasRequestMod(mod)) return;
        synchronized(requestLocker) {
            RawMod[] newRequestMods = new RawMod[requestMods.length + 1];
            System.arraycopy(requestMods, 0, newRequestMods, 0, requestMods.length);
            newRequestMods[requestMods.length] = mod;
            requestMods = newRequestMods;
        }
        save();
    }

    public void removeRequestMod(int i) throws IOException {
        synchronized(requestLocker) {
            RawMod[] newRequestMods = new RawMod[requestMods.length - 1];
            System.arraycopy(requestMods, 0, newRequestMods, 0, i);
            System.arraycopy(requestMods, i + 1, newRequestMods, i, requestMods.length - i - 1);
            requestMods = newRequestMods;
        }
        save();
    }

    public void changeRequestMod(int i, RawMod mod) throws IOException {
        synchronized(requestLocker) {
            requestMods[i] = mod;
        }
        save();
    }

    public boolean hasRequestMod(RawMod mod) {
        for(RawMod requestMod : requestMods) if(requestMod.equals(mod)) return true;
        return false;
    }

    public RawMod[] getAndRemoveRequestMods() throws IOException {
        synchronized(requestLocker) {
            RawMod[] requestMods = this.requestMods;
            this.requestMods = new RawMod[0];
            save();
            return requestMods;
        }
    }

    public void resetRequestMods() throws IOException {
        synchronized(requestLocker) {
            requestMods = new RawMod[0];
            save();
        }
    }
}
