package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.data.DiscordUserData;
import kr.jongyeol.jaServer.data.RawMod;
import kr.jongyeol.jaServer.data.UserData;
import kr.jongyeol.jaServer.exception.GetBinaryException;
import kr.jongyeol.jaServer.packet.ByteArrayDataInput;
import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;
import kr.jongyeol.jaServer.packet.RequestPacket;
import kr.jongyeol.jaServer.packet.ResponsePacket;
import kr.jongyeol.jaServer.packet.request.ConnectInfo;
import kr.jongyeol.jaServer.packet.response.PacketResponseError;
import kr.jongyeol.jaServer.packet.response.DownloadModRequest;
import lombok.Cleanup;
import org.springframework.web.socket.BinaryMessage;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.BinaryWebSocketHandler;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

public class Connection extends BinaryWebSocketHandler {
    public static List<Connection> connections = new ArrayList<>();
    public Logger logger;
    private WebSocketSession session;
    public ConnectInfo connectInfo;

    @Override
    public void afterConnectionEstablished(WebSocketSession session) throws Exception {
        try {
            this.session = session;
            session.setBinaryMessageSizeLimit(1024 * 8); // TODO: Set limit
            String ip = session.getUri().getHost();
            if(session.getHandshakeHeaders().containsKey("X-Forwarded-For"))
                ip = session.getHandshakeHeaders().get("X-Forwarded-For").get(0);
            logger = new Logger(ip, "connect");
            Logger.MAIN_LOGGER.info(ip + " 로그를 생성하였습니다.");
            logger.info(ip + "가 연결되었습니다.");
        } catch (Exception e) {
            logger.error("Error in Connection");
            logger.error(e);
            session.close();
            throw new RuntimeException(e);
        }
    }

    @Override
    protected void handleBinaryMessage(WebSocketSession session, BinaryMessage message) throws Exception {
        Variables.executor.execute(() -> {
            @Cleanup ByteArrayDataInput input = new ByteArrayDataInput(GZipFile.gunzipData(message.getPayload().array()));
            String method = new String(input.readBytes(), StandardCharsets.UTF_8);
            long id = input.readLong();
            try {
                Class<RequestPacket> requestPacketClass = (Class<RequestPacket>) Class.forName("kr.jongyeol.jaServer.packet.request." + method);
                RequestPacket requestPacket = requestPacketClass.getDeclaredConstructor().newInstance();
                requestPacket.id = id;
                requestPacket.getData(this, input);
                sendData(requestPacket);
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
        @Cleanup ByteArrayDataOutput output = new ByteArrayDataOutput();
        if(responsePacket instanceof RequestPacket requestPacket) {
            output.writeBoolean(true);
            output.writeLong(requestPacket.id);
        } else {
            output.writeBoolean(false);
            output.writeUTF(responsePacket.getClass().getSimpleName());
        }
        try {
            responsePacket.getBinary(output);
        } catch (Exception e) {
            throw new GetBinaryException(responsePacket.getClass().getSimpleName(), e);
        }
        session.sendMessage(new BinaryMessage(GZipFile.gzipData(output.toByteArray())));
    }

    public boolean isClosed() {
        return !session.isOpen();
    }

    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) throws Exception {
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
