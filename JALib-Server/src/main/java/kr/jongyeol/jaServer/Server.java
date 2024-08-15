package kr.jongyeol.jaServer;

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
    }

    public static void BootstrapRun(String[] args) throws Exception {
        if(loaded) return;
        File file = new File("library");
        if(!file.exists()) file.mkdir();
        List<URL> urls = new ArrayList<>();
        for(File f : file.listFiles()) {
            try {
                urls.add(f.toURI().toURL());
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
        URLClassLoader loader = new URLClassLoader(urls.toArray(new URL[0]), Server.class.getClassLoader());
        loader.loadClass("kr.jongyeol.jaServer.Boot").getMethod("run", String[].class).invoke(null, (Object) args);
        loaded = true;
    }
}
