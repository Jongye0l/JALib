package kr.jongyeol.jaServer;

import com.google.gson.*;
import kr.jongyeol.jaServer.data.CustomDownloadLink;
import kr.jongyeol.jaServer.data.DownloadLink;
import kr.jongyeol.jaServer.data.GithubDownloadLink;
import kr.jongyeol.jaServer.data.Version;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class Variables {
    public static final Gson gson = new GsonBuilder()
        .registerTypeAdapter(DownloadLink.class, (JsonSerializer<DownloadLink>) (src, typeOfSrc, context) -> {
            JsonObject object = new JsonObject();
            if(src instanceof GithubDownloadLink githubDownloadLink) {
                object.addProperty("type", "github");
                object.addProperty("modName", githubDownloadLink.modName);
            } else if(src instanceof CustomDownloadLink customDownloadLink) {
                object.addProperty("type", "custom");
                for(Version key : customDownloadLink.links.keySet()) object.addProperty(key.toString(), customDownloadLink.links.get(key));
            }
            return object;
        }).registerTypeAdapter(DownloadLink.class, (JsonDeserializer<DownloadLink>) (json, typeOfT, context) -> {
            JsonObject object = json.getAsJsonObject();
            String type = object.get("type").getAsString();
            if(type.equals("github")) return new GithubDownloadLink(object.get("modName").getAsString());
            else if(type.equals("custom")) {
                Map<Version, String> links = new HashMap<>();
                for(String key : object.keySet()) {
                    if(key.equals("type")) continue;
                    links.put(new Version(key), object.get(key).getAsString());
                }
                return new CustomDownloadLink(links);
            }
            return null;
        }).registerTypeAdapter(Version.class, (JsonSerializer<Version>) (src, typeOfSrc, context) -> new JsonPrimitive(src.toString()))
        .registerTypeAdapter(Version.class, (JsonDeserializer<Version>) (json, typeOfT, context) -> new Version(json.getAsString()))
        .create();

    public static ExecutorService executor = Executors.newFixedThreadPool(4);
}
