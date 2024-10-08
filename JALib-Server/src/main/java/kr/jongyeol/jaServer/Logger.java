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
import java.util.Calendar;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

public class Logger {
    public static final Logger MAIN_LOGGER;
    private static final File logFolder;
    private static final DateTimeFormatter logFormat = DateTimeFormatter.ofPattern("HH:mm:ss");
    private static final Map<String, Logger> loggerMap = new HashMap<>();

    static {
        logFolder = new File(Settings.getInstance().getLogPath());
        if(!logFolder.exists()) logFolder.mkdir();
        MAIN_LOGGER = Logger.createLogger("Main");
    }

    @Getter
    private String name;
    @Getter
    private String category;
    private Date lastDate;
    private int connectionCount = 1;
    private final Object saveLocker = new Object();
    private final Object sendLocker = new Object();

    private Logger(String name, String category) {
        this.name = name;
        this.category = category;
        loadFile();
        loggerMap.put(name, this);
        if(MAIN_LOGGER != null) MAIN_LOGGER.info("Logger " + name + " created.");
    }

    private Logger addConnection() {
        connectionCount++;
        MAIN_LOGGER.info("Logger " + name + " connect added(" + connectionCount + ").");
        return this;
    }

    public static Logger createLogger(String name) {
        return createLogger(name, null);
    }

    public static Logger createLogger(String name, String category) {
        return loggerMap.containsKey(name) ? loggerMap.get(name).addConnection() : new Logger(name, category);
    }

    public static Logger getLogger(String name) {
        return loggerMap.get(name);
    }

    public static void closeAll() {
        for(Logger logger : loggerMap.values().toArray(new Logger[0])) logger.close();
    }

    private void loadFile() {
        File folder = category == null ? logFolder : new File(logFolder, category);
        if(!folder.exists()) folder.mkdir();
        lastDate = new Date();
        File file = new File(folder, getFileName());
        if(file.exists()) gzip();
        try {
            file.createNewFile();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private String getFileName() {
        return String.format("%s-%s.log", name, new SimpleDateFormat("yyyy-MM-dd").format(lastDate));
    }

    @SneakyThrows(IOException.class)
    private void gzip() {
        File folder = category == null ? logFolder : new File(logFolder, category);
        String defaultName = getFileName().replace(".log", "");
        File file;
        File original = new File(folder, defaultName + ".log");
        int i = 1;
        do file = new File(folder, String.format("%s-%d.log.gz", defaultName, i++));
        while(file.exists());
        GZipFile.gzipFile(original, file);
        original.delete();
    }

    private void log(String type, String s) {
        synchronized(sendLocker) {
            String result = String.format("[%s] [%s/%s] %s", LocalTime.now().format(logFormat), name, type, s);
            System.out.println(result);
            addQueue(result);
            if(this != MAIN_LOGGER) MAIN_LOGGER.addQueue(result);
        }
    }

    private void addQueue(String s) {
        Variables.executor.execute(() -> {
            Path path = category == null ? Path.of(logFolder.getPath(), getFileName()) : Path.of(logFolder.getPath(), category, getFileName());
            synchronized(saveLocker) {
                try {
                    if(lastDate.getDate() != Calendar.getInstance().get(Calendar.DATE) || Files.size(path) > 1048576) {
                        gzip();
                        loadFile();
                    }
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
        synchronized(sendLocker) {
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
        if(--connectionCount > 0) {
            if(MAIN_LOGGER != null) MAIN_LOGGER.info("Logger " + name + " connect removed(" + connectionCount + ").");
            return;
        }
        loggerMap.remove(name, this);
        if(MAIN_LOGGER != null) MAIN_LOGGER.info("Logger " + name + " closed.");
        Variables.executor.execute(() -> {
            synchronized(saveLocker) {
                gzip();
            }
        });
    }
}
