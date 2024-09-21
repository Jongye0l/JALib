package kr.jongyeol.jaServer.controller;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import kr.jongyeol.jaServer.Logger;
import kr.jongyeol.jaServer.data.TokenData;

public class CustomController {

    protected boolean checkPermission(HttpServletResponse response, HttpServletRequest request) {
        return checkPermission(response, getTokenData(request));
    }

    protected boolean checkPermission(HttpServletResponse response, String token) {
        if(token == null || !TokenData.getTokens().contains(token)) {
            response.setStatus(403);
            return true;
        }
        return false;
    }

    protected String getTokenData(HttpServletRequest request) {
        return request.getHeader("token");
    }

    public static void info(HttpServletRequest request, String string) {
        String ip = getIp(request);
        Logger logger = Logger.getLogger(ip);
        if(logger == null) Logger.MAIN_LOGGER.info(ip + ": " + string);
        else logger.info(string);
    }

    public static void error(HttpServletRequest request, String string) {
        String ip = getIp(request);
        Logger logger = Logger.getLogger(ip);
        if(logger == null) Logger.MAIN_LOGGER.error(ip + ": " + string);
        else logger.error(string);
    }

    public static void error(HttpServletRequest request, Exception e) {
        String ip = getIp(request);
        Logger logger = Logger.getLogger(ip);
        if(logger == null) Logger.MAIN_LOGGER.error(e, ip);
        else logger.error(e);
    }

    public static String getIp(HttpServletRequest request) {
        String ipAddress = request.getHeader("X-Forwarded-For");
        if(ipAddress == null || ipAddress.isEmpty() || "unknown".equalsIgnoreCase(ipAddress))
            ipAddress = request.getRemoteAddr();
        else ipAddress = ipAddress.split(",")[0];
        return ipAddress;
    }
}
