package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.data.DiscordUserData;
import kr.jongyeol.jaServer.data.RawMod;
import kr.jongyeol.jaServer.data.UserData;
import kr.jongyeol.jaServer.exception.GetBinaryException;
import kr.jongyeol.jaServer.packet.RequestPacket;
import kr.jongyeol.jaServer.packet.ResponsePacket;
import kr.jongyeol.jaServer.packet.request.ConnectInfo;
import kr.jongyeol.jaServer.packet.response.PacketResponseError;
import kr.jongyeol.jaServer.packet.response.DownloadModRequest;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class Connection {
    public static List<Connection> connections = new ArrayList<>();
    private static final String ADMIN_LOGIN = "admin.AdminLogin";
    public Logger logger;
    public boolean isAdmin;
    private Socket socket;
    private InputStream in;
    private OutputStream out;
    public ConnectInfo connectInfo;
    private Thread thread;
    private byte[] intBuffer = new byte[4];
    private byte[] longBuffer = new byte[8];
    private final Object senderLocker = new Object();

    public static void connect(Socket socket) {
        try {
            connections.add(new Connection(socket, false));
        } catch (Exception ignored) {
        }
    }

    public Connection(Socket socket, boolean isAdmin) {
        try {
            this.socket = socket;
            this.isAdmin = isAdmin;
            in = socket.getInputStream();
            out = socket.getOutputStream();
            String ip = socket.getInetAddress().getHostAddress();
            logger = new Logger(ip, "connect");
            Logger.MAIN_LOGGER.info(ip + " 로그를 생성하였습니다.");
            logger.info(ip + "가 연결되었습니다.");
            thread = new Thread(this::threadRead);
            thread.start();
        } catch (Exception e) {
            logger.error("Error in Connection");
            logger.error(e);
            close();
            throw new RuntimeException(e);
        }
    }

    private void threadRead() {
        try {
            while(!Thread.currentThread().isInterrupted()) readAction();
        } catch (IOException e) {
            close();
        } catch (Exception e) {
            if(logger != null) {
                logger.error("Error in Connection");
                logger.error(e);
            }
            close();
        }
    }

    private void readAction() throws IOException {
        String method = new String(readBytes(), StandardCharsets.UTF_8);
        boolean adminLogin = method.equals(ADMIN_LOGIN);
        long id = isAdmin || adminLogin ? 0L : readLong();
        byte[] data = readBytes();
        Variables.executor.execute(() -> {
            try {
                if(!isAdmin && !adminLogin && method.startsWith("admin.")) {
                    logger.error("This Connection is not Admin but try to use Admin Packet: " + method);
                    close();
                    return;
                }
                Class<RequestPacket> requestPacketClass = (Class<RequestPacket>) Class.forName("kr.jongyeol.jaServer.packet.request." + method);
                RequestPacket requestPacket = requestPacketClass.getDeclaredConstructor().newInstance();
                requestPacket.id = id;
                requestPacket.getData(this, data);
                if(!isAdmin) sendData(requestPacket);
            } catch (Exception e) {
                if(logger == null) return;
                logger.error("Error in " + method + " packet");
                logger.error(e);
                try {
                    sendData(new PacketResponseError(id, e));
                } catch (Exception ex) {
                    if(logger == null) return;
                    logger.error(ex);
                }
            }
        });
    }

    public void sendData(ResponsePacket responsePacket) throws Exception {
        byte[] binary;
        try {
            binary = responsePacket.getBinary();
        } catch (Exception e) {
            throw new GetBinaryException(responsePacket.getClass().getSimpleName(), e);
        }
        synchronized(senderLocker) {
            if(isAdmin) {
                write(responsePacket.getClass().getSimpleName().getBytes(StandardCharsets.UTF_8));
                write(binary);
            } else if(responsePacket instanceof RequestPacket requestPacket) {
                write(true);
                write(requestPacket.id);
                write(binary);
            } else {
                write(false);
                write(responsePacket.getClass().getSimpleName().getBytes(StandardCharsets.UTF_8));
                write(binary);
            }
        }
    }

    private byte[] readBytes() throws IOException {
        int size;
        synchronized(intBuffer) {
            readBytes(intBuffer);
            size = ((intBuffer[0] << 24) + (intBuffer[1] << 16) + (intBuffer[2] << 8) + (intBuffer[3]));
        }
        return readBytes(new byte[size]);
    }

    private byte[] readBytes(byte[] value) throws IOException {
        int cur = 0;
        while(cur < value.length) {
            int i = in.read(value, cur, value.length - cur);
            if(i == -1) throw new IOException();
            cur += i;
        }
        return value;
    }

    private long readLong() {
        try {
            synchronized(longBuffer) {
                readBytes(longBuffer);
                return (((long) longBuffer[0] << 56)
                    + ((long) longBuffer[1] << 48)
                    + ((long) longBuffer[2] << 40)
                    + ((long) longBuffer[3] << 32)
                    + (longBuffer[4] << 24)
                    + (longBuffer[5] << 16)
                    + (longBuffer[6] << 8)
                    + (longBuffer[7]));
            }
        } catch (IOException e) {
            if(logger != null) {
                logger.error(e);
                close();
            }
            throw new RuntimeException(e);
        }
    }

    public void write(byte[] data) {
        try {
            synchronized(intBuffer) {
                intBuffer[0] = (byte) (data.length >>> 24);
                intBuffer[1] = (byte) (data.length >>> 16);
                intBuffer[2] = (byte) (data.length >>> 8);
                intBuffer[3] = (byte) (data.length);
                out.write(intBuffer);
            }
            out.write(data);
        } catch (IOException e) {
            if(logger != null) {
                logger.error(e);
                close();
            }
            throw new RuntimeException(e);
        }
    }

    public void write(long data) {
        try {
            synchronized(longBuffer) {
                longBuffer[0] = (byte) (data >>> 56);
                longBuffer[1] = (byte) (data >>> 48);
                longBuffer[2] = (byte) (data >>> 40);
                longBuffer[3] = (byte) (data >>> 32);
                longBuffer[4] = (byte) (data >>> 24);
                longBuffer[5] = (byte) (data >>> 16);
                longBuffer[6] = (byte) (data >>> 8);
                longBuffer[7] = (byte) (data);
                out.write(longBuffer);
            }
        } catch (IOException e) {
            if(logger != null) {
                logger.error(e);
                close();
            }
            throw new RuntimeException(e);
        }
    }

    public void write(boolean data) {
        try {
            out.write(data ? 1 : 0);
        } catch (IOException e) {
            if(logger != null) {
                logger.error(e);
                close();
            }
            throw new RuntimeException(e);
        }
    }

    public boolean isClosed() {
        return socket.isClosed();
    }

    private void close() {
        if(in != null) try {
            in.close();
        } catch (IOException ignored) {
        }
        if(out != null) try {
            out.close();
        } catch (IOException ignored) {
        }
        if(socket != null) try {
            socket.close();
        } catch (IOException ignored) {
        }
        in = null;
        out = null;
        socket = null;
        if(thread != null) thread.interrupt();
        thread = null;
        intBuffer = null;
        longBuffer = null;
        connectInfo = null;
        connections.remove(this);
        if(logger != null) {
            logger.info("연결이 종료되었습니다.");
            logger.close();
        }
        logger = null;
    }

    public void loadModRequest() {
        UserData.getUserData(connectInfo.steamID).forEach(l -> {
            try {
                for(RawMod rawMod : DiscordUserData.getUserData(l).getAndRemoveRequestMods()) {
                    DownloadModRequest downloadModRequest = new DownloadModRequest(rawMod);
                    sendData(downloadModRequest);
                }
            } catch (Exception e) {
                if(logger != null) logger.error(e);
            }
        });
    }
}
