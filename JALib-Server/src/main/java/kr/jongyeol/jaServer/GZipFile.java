package kr.jongyeol.jaServer;

import lombok.Cleanup;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.zip.GZIPOutputStream;

public class GZipFile {
    public static void gzipFile(File source, File destination) throws IOException {
        byte[] buffer = new byte[1024];
        @Cleanup GZIPOutputStream gzipOuputStream = new GZIPOutputStream(new FileOutputStream(destination));
        @Cleanup FileInputStream fileInput = new FileInputStream(source);
        int bytes_read;
        while((bytes_read = fileInput.read(buffer)) > 0) gzipOuputStream.write(buffer, 0, bytes_read);
        gzipOuputStream.finish();
    }
}
