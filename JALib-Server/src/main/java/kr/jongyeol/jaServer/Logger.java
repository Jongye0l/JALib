package kr.jongyeol.jaServer;

import lombok.Getter;
import lombok.SneakyThrows;
import lombok.extern.java.Log;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.text.SimpleDateFormat;
import java.time.LocalDate;
import java.time.LocalTime;
import java.time.format.DateTimeFormatter;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;

public class Logger {
    public static final Logger MAIN_LOGGER;
    private static final File logFolder;
    private static final SimpleDateFormat logFileDateFormat = new SimpleDateFormat("yyyy-MM-dd");
    private static final DateTimeFormatter logFormat = DateTimeFormatter.ofPattern("HH:mm:ss");
    private static final Map<String, Logger> loggerMap = new HashMap<>();

    static {
        logFolder = new File(Settings.getInstance().getLogPath());
        if(!logFolder.exists()) logFolder.mkdir();
        MAIN_LOGGER = new Logger("Main");
    }

    @Getter
    private String name;

    @Getter
    private String category;
    private File file;
    private LocalDate lastDate;
    private Path path;
    private Object locker = new Object();

    public Logger(String name) {
        this(name, null);
    }

    public Logger(String name, String category) {
        this.name = name;
        this.category = category;
        loadFile();
        loggerMap.put(name, this);
    }

    public static Logger getLogger(String name) {
        return loggerMap.get(name);
    }

    @SneakyThrows(IOException.class)
    private void loadFile() {
        File folder = category == null ? logFolder : new File(logFolder, category);
        if(!folder.exists()) folder.mkdir();
        lastDate = LocalDate.now();
        String date = logFileDateFormat.format(new Date());
        if(file == null) file = new File(folder, String.format("%s-%s.log", name, date));
        if(file.exists()) {
            String defaultName = file.getName().replace(".log", "");
            File file = null;
            int i = 1;
            while(file == null || file.exists())
                file = new File(folder, String.format("%s-%d.log.gz", defaultName, i++));
            GZipFile.gzipFile(this.file, file);
            this.file.delete();
        }
        try {
            file.createNewFile();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
        path = file.toPath();
    }

    private void log(String type, String s) {
        String result = String.format("[%s] [%s/%s] %s", LocalTime.now().format(logFormat), name, type, s);
        System.out.println(result);
        addQueue(result);
        if(this != MAIN_LOGGER) MAIN_LOGGER.addQueue(result);
    }

    private void addQueue(String s) {
        Variables.executor.execute(() -> {
            synchronized(locker) {
                try {
                    if(lastDate.isBefore(LocalDate.now()) || Files.size(path) > 1048576) loadFile();
                    Files.writeString(path, s + "\n", StandardOpenOption.APPEND);
                } catch (OutOfMemoryError ignored) {
                } catch (IOException e) {
                    MAIN_LOGGER.error(e);
                }
            }
        });
    }

    public void info(String s) {
        log("INFO", s);
    }

    public void info(Object o) {
        info(o == null ? "null" : o.toString());
    }

    public void warn(String s) {
        log("WARN", s);
    }

    public void warn(Object o) {
        warn(o == null ? "null" : o.toString());
    }

    public void error(String s) {
        log("ERROR", s);
    }

    public void error(Throwable e) {
        error(e, "");
    }

    public void error(Throwable e, String prefix) {
        synchronized(locker) {
            error(prefix + e.toString());
            for(StackTraceElement stackTrace : e.getStackTrace())
                error(String.format("\tat %s.%s(%s:%d) [%s:%s]", stackTrace.getClassName(), stackTrace.getMethodName(),
                    stackTrace.getFileName(), stackTrace.getLineNumber(), stackTrace.getModuleName(), stackTrace.getModuleVersion()));
            for(Throwable se : e.getSuppressed()) error(se, "Suppressed: ");
            if(e.getCause() != null) error(e.getCause(), "Caused by: ");
        }
    }

    public void error(Object o) {
        if(o instanceof Throwable e) error(e);
        else if(o == null) error("null");
        else error(o.toString());
    }

    public void close() {
        loggerMap.remove(name, this);
        Variables.executor.execute(() -> {
            synchronized(locker) {
                Variables.setNull(this);
            }
        });
    }
}
