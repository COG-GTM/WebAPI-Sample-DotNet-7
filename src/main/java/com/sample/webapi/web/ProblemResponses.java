package com.sample.webapi.web;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;

/**
 * Builds RFC 7807 {@code application/problem+json} responses with the same members the original
 * ASP.NET Core API returned ({@code type}, {@code title}, {@code status}, {@code traceId}, optional {@code errors}).
 */
final class ProblemResponses {

    static final String VALIDATION_TITLE = "One or more validation errors occurred.";

    private static final Map<Integer, String[]> TYPES = Map.of(
            400, new String[] {"https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"},
            404, new String[] {"https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"},
            405, new String[] {"https://tools.ietf.org/html/rfc7231#section-6.5.5", "Method Not Allowed"},
            406, new String[] {"https://tools.ietf.org/html/rfc7231#section-6.5.6", "Not Acceptable"},
            415, new String[] {"https://tools.ietf.org/html/rfc7231#section-6.5.13", "Unsupported Media Type"},
            500, new String[] {"https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    "An error occurred while processing your request."});

    private ProblemResponses() {
    }

    static ResponseEntity<Object> of(HttpStatusCode status) {
        return of(status, HttpHeaders.EMPTY, null, null);
    }

    static ResponseEntity<Object> validation(Map<String, List<String>> errors) {
        return of(HttpStatus.BAD_REQUEST, HttpHeaders.EMPTY, VALIDATION_TITLE, errors);
    }

    static ResponseEntity<Object> of(HttpStatusCode status, HttpHeaders headers, String title,
            Map<String, List<String>> errors) {
        String[] typeAndTitle = TYPES.get(status.value());
        Map<String, Object> body = new LinkedHashMap<>();
        if (typeAndTitle != null) {
            body.put("type", typeAndTitle[0]);
        }
        String resolvedTitle = title != null ? title
                : typeAndTitle != null ? typeAndTitle[1]
                : status instanceof HttpStatus known ? known.getReasonPhrase() : null;
        if (resolvedTitle != null) {
            body.put("title", resolvedTitle);
        }
        body.put("status", status.value());
        body.put("traceId", UUID.randomUUID().toString());
        if (errors != null) {
            body.put("errors", errors);
        }
        return ResponseEntity.status(status).headers(headers).contentType(MediaType.APPLICATION_PROBLEM_JSON).body(body);
    }
}
