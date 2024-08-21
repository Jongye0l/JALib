package kr.jongyeol.jaServer;

import kr.jongyeol.jaServer.packet.ByteArrayDataOutput;

import java.nio.ByteBuffer;
import java.util.zip.Deflater;
import java.util.zip.Inflater;

public class Compress {
    public static byte[] compress(byte[] data) {
        Deflater deflater = new Deflater(Deflater.BEST_COMPRESSION);
        deflater.setInput(data);
        deflater.finish();
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        byte[] buffer = new byte[1024];
        while(!deflater.finished()) {
            int count = deflater.deflate(buffer);
            output.writeBytes(buffer, 0, count);
        }
        deflater.end();
        return output.toByteArray();
    }

    public static byte[] decompress(byte[] data) {
        Inflater inflater = new Inflater();
        inflater.setInput(data);
        ByteArrayDataOutput output = new ByteArrayDataOutput();
        byte[] buffer = new byte[1024];
        try {
            while (!inflater.finished()) {
                int count = inflater.inflate(buffer);
                output.writeBytes(buffer, 0, count);
            }
        } catch (Exception e) {
            Logger.MAIN_LOGGER.error(e);
        }
        inflater.end();
        return output.toByteArray();
    }
}
