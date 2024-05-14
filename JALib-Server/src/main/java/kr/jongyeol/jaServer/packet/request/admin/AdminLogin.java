package kr.jongyeol.jaServer.packet.request.admin;

import kr.jongyeol.jaServer.Connection;
import kr.jongyeol.jaServer.Settings;
import kr.jongyeol.jaServer.packet.RequestPacket;

import java.nio.charset.StandardCharsets;

public class AdminLogin extends RequestPacket {
    @Override
    public void getData(Connection connection, byte[] data) throws Exception {
        String password = new String(data, StandardCharsets.UTF_8);
        if(password.equals(Settings.instance.adminPassword)){
            connection.isAdmin = true;
            connection.logger.info("Admin Login Succss");
        }
        connection.logger.info("Admin Login Failed");
    }

    @Override
    public byte[] getBinary() throws Exception {
        return Settings.instance.adminPassword.getBytes(StandardCharsets.UTF_8);
    }
}
