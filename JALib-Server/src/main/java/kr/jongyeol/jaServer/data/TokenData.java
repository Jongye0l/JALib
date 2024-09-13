package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.Variables;
import lombok.SneakyThrows;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public class TokenData {
    private static List<String> tokens;
    private static AutoRemovedData autoRemovedData;

    public static void LoadToken() throws IOException {
        List<String> oldTokens = tokens;
        try {
            tokens = new ArrayList<>();
            Path path = Path.of(Settings.getInstance().getTokenPath());
            String data = Files.readString(path);
            Collections.addAll(tokens, data.split("\n"));
            if(autoRemovedData == null) {
                autoRemovedData = new AutoRemovedData() {
                    @Override
                    public void onRemove() {
                        tokens.clear();
                        Variables.setNull(tokens);
                        tokens = null;
                    }
                };
            } else autoRemovedData.use();
        } catch (Exception e) {
            tokens = oldTokens;
            throw e;
        }
    }

    @SneakyThrows
    public static List<String> getTokens() {
        if(tokens == null) LoadToken();
        autoRemovedData.use();
        return tokens;
    }
}
