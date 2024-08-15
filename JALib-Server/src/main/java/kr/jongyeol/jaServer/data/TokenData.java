package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.Settings;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public class TokenData {
    public static List<String> tokens;

    public static void LoadToken() throws IOException {
        List<String> oldTokens = tokens;
        try {
            tokens = new ArrayList<>();
            Path path = Path.of(Settings.instance.tokenPath);
            String data = Files.readString(path);
            Collections.addAll(tokens, data.split("\n"));
        } catch (Exception e) {
            tokens = oldTokens;
            throw e;
        }
    }
}
