package kr.jongyeol.jaServer;

import java.net.ServerSocket;

public class Server {
    public static ServerSocket serverSocket;

    public static void main(String[] args) {
        try {
            System.setErr(new ErrorStream(System.err));
            int port = Settings.instance.port;
            serverSocket = new ServerSocket(port);
            Logger.MAIN_LOGGER.info("서버가 " + port + " 포트로 열렸습니다.");
            while(true) Connection.connect(serverSocket.accept());
        } catch (Exception e) {
            e.printStackTrace();
            Logger.MAIN_LOGGER.error(e);
        }
    }
}
