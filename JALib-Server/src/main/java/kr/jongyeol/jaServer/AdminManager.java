package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.packet.RequestPacket;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

public class AdminManager {
    public static Connection connection;
    public static List<RequestPacket> requestPackets;

    public static void setConnection(Connection connection) throws IOException {
        AdminManager.connection = connection;
        for(RequestPacket packet : requestPackets) try {
            connection.sendData(packet);
        } catch (Exception e) {
            connection.logger.error(e);
        }
        requestPackets.clear();
        requestPackets = null;
        Files.delete(Path.of(Settings.instance.adminManagerPath));
    }

    public static void sendPacket(RequestPacket packet) {
        if(connection == null || connection.isClosed()) {
            if(requestPackets == null) requestPackets = new ArrayList<>();
            requestPackets.add(packet);
        } else try {
            connection.sendData(packet);
        } catch (Exception e) {
            connection.logger.error(e);
        }
    }
}
