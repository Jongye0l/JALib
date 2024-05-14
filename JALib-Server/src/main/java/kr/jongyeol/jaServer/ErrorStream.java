package kr.jongyeol.jaServer;

import java.io.OutputStream;
import java.io.PrintStream;

public class ErrorStream extends PrintStream {
    public ErrorStream(OutputStream out) {
        super(out);
    }

    @Override
    public void println(String x) {
        Logger.MAIN_LOGGER.error(x);
    }

    @Override
    public void println(Object x) {
        Logger.MAIN_LOGGER.error(x == null ? "null" : x.toString());
    }
}
