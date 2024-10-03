package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.data.ModData;
import lombok.SneakyThrows;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

import java.io.File;
import java.net.URL;
import java.net.URLClassLoader;
import java.util.ArrayList;
import java.util.List;

@SpringBootApplication
public class Server {
    public static boolean loaded = false;

    public static void main(String[] args) throws Exception {
        System.setErr(new ErrorStream(System.err));
        SpringApplication.run(Server.class, args);
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            Logger.MAIN_LOGGER.info("서버가 종료됩니다.");
            Logger.closeAll();
        }, "ShutdownHook"));
    }

    public static void bootstrapRun(String[] args) throws Exception {
        if(loaded) return;
        File file = new File("library");
        if(!file.exists()) {
            loaded = true;
            nonLibrary();
            return;
        }
        List<URL> urls = new ArrayList<>();
        for(File f : file.listFiles()) {
            try {
                urls.add(f.toURI().toURL());
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
        if(urls.isEmpty()) {
            loaded = true;
            nonLibrary();
            return;
        }
        URLClassLoader loader = new URLClassLoader(urls.toArray(new URL[0]), Server.class.getClassLoader());
        loader.loadClass("kr.jongyeol.jaServer.Boot").getMethod("run", String[].class).invoke(null, (Object) args);
        loaded = true;
    }

    @SneakyThrows
    public static void nonLibrary() {
        ModData.LoadModData();
        ConnectOtherLib.setupModData();
    }
}
