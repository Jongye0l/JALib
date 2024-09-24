package kr.jongyeol.jaServer.data;

import com.google.gson.reflect.TypeToken;
import kr.jongyeol.jaServer.Logger;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.Variables;
import lombok.SneakyThrows;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class UserData {
    private static Map<Long, List<Long>> userDataMap;
    private static AutoRemovedData autoRemovedData;

    @SneakyThrows(IOException.class)
    public static void checkLoad() {
        if(userDataMap != null) {
            autoRemovedData.use();
            return;
        }
        userDataMap = Variables.gson.fromJson(Files.readString(Path.of(Settings.getInstance().getUserDataPath())), new TypeToken<Map<Long, List<Long>>>(){}.getType());
        autoRemovedData = new AutoRemovedData() {
            @Override
            public void onRemove() {
                try {
                    save();
                    userDataMap.clear();
                    userDataMap = null;
                    autoRemovedData = null;
                } catch (IOException e) {
                    Logger.MAIN_LOGGER.error(e);
                }
            }
        };
    }

    public static void save() throws IOException {
        Files.writeString(Path.of(Settings.getInstance().getUserDataPath()), Variables.gson.toJson(userDataMap));
    }

    public static List<Long> getUserData(long id) {
        checkLoad();
        return userDataMap.get(id);
    }

    public static void addDiscordID(long steamID, long discordID) throws IOException {
        checkLoad();
        List<Long> list = userDataMap.computeIfAbsent(steamID, k -> new ArrayList<>());
        if(!list.contains(discordID)) {
            list.add(discordID);
            DiscordUserData.getUserData(discordID).setSteamID(steamID);
            save();
        }
    }

    public static void removeDiscordID(long steamID, long discordID) throws IOException {
        checkLoad();
        List<Long> list = userDataMap.get(steamID);
        if(list != null) {
            list.remove(discordID);
            save();
        }
    }
}
