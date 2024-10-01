package kr.jongyeol.jaServer.controller;

import jakarta.servlet.http.HttpServletRequest;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
public class ControllerExceptionHandler extends CustomController {
    @ExceptionHandler(Exception.class)
    public ResponseEntity<?> handleCustomException(HttpServletRequest request, Exception th) {
        error(request, th);
        return new ResponseEntity<>(HttpStatus.BAD_REQUEST);
    }
}
