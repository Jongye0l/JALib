package kr.jongyeol.jaServer;

import lombok.SneakyThrows;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.ApplicationListener;
import org.springframework.stereotype.Component;

@Component
public class ApplicationStartupListener implements ApplicationListener<ApplicationReadyEvent> {

    @SneakyThrows
    @Override
    public void onApplicationEvent(ApplicationReadyEvent event) {
        Server.BootstrapRun(new String[0]);
        ConnectOtherLib.setupModData();
        ConnectOtherLib.loadModRequest();
    }
}