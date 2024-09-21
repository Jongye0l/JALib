package kr.jongyeol.jaServer;

import lombok.Cleanup;

import java.io.*;
import java.util.zip.Deflater;
import java.util.zip.GZIPInputStream;
import java.util.zip.GZIPOutputStream;

public class GZipFile {
    public static void gzipFile(File source, File destination) throws IOException {
        byte[] buffer = new byte[1024];
        @Cleanup GZIPOutputStream gzipOuputStream = new GZIPOutputStream(new FileOutputStream(destination)) {{
            def.setLevel(Deflater.BEST_COMPRESSION);
        }};
        @Cleanup FileInputStream fileInput = new FileInputStream(source);
        int bytes_read;
        while((bytes_read = fileInput.read(buffer)) > 0) gzipOuputStream.write(buffer, 0, bytes_read);
        gzipOuputStream.finish();
    }

    public static byte[] gzipData(byte[] data) {
        try {
            ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
            @Cleanup GZIPOutputStream gzipOuputStream = new GZIPOutputStream(byteArrayOutputStream) {{
                def.setLevel(Deflater.BEST_COMPRESSION);
            }};
            gzipOuputStream.write(data);
            gzipOuputStream.finish();
            return byteArrayOutputStream.toByteArray();
        } catch (IOException e) {
            Logger.MAIN_LOGGER.error(e);
            return null;
        }
    }

    public static byte[] gunzipData(byte[] data) {
        try {
            ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
            ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(data);
            @Cleanup GZIPInputStream gzipInputStream = new GZIPInputStream(byteArrayInputStream);
            byte[] buffer = new byte[1024];
            int bytes_read;
            while((bytes_read = gzipInputStream.read(buffer)) > 0) byteArrayOutputStream.write(buffer, 0, bytes_read);
            return byteArrayOutputStream.toByteArray();
        } catch (IOException e) {
            Logger.MAIN_LOGGER.error(e);
            return null;
        }
    }
}
