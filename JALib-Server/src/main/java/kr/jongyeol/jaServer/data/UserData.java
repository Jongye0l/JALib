package kr.jongyeol.jaServer.data;

import com.google.gson.reflect.TypeToken;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.Variables;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class UserData {
    private static Map<Long, List<Long>> userDataMap;

    static {
        try {
            userDataMap = Variables.gson.fromJson(Files.readString(Path.of(Settings.instance.userDataPath)), new TypeToken<Map<Long, List<Long>>>(){}.getType());
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public static void save() throws IOException {
        Files.writeString(Path.of(Settings.instance.userDataPath), Variables.gson.toJson(userDataMap));
    }

    public static List<Long> getUserData(long id) {
        return userDataMap.get(id);
    }

    public static void addDiscordID(long steamID, long discordID) throws IOException {
        List<Long> list = userDataMap.computeIfAbsent(steamID, k -> new ArrayList<>());
        if(!list.contains(discordID)) list.add(discordID);
        save();
        DiscordUserData.getUserData(discordID).setSteamID(steamID);
    }

    public static void removeDiscordID(long steamID, long discordID) throws IOException {
        List<Long> list = userDataMap.get(steamID);
        if(list != null) {
            list.remove(discordID);
            save();
        }
    }
}
