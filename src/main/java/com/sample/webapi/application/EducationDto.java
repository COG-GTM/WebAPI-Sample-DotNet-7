package com.sample.webapi.application;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotNull;
import java.util.UUID;

/**
 * Education payload. {@code degree}, {@code fieldOfStudy} and {@code school} must be present and non-null
 * (empty strings are accepted, as in the original API); length limits are enforced by the database.
 */
@Schema(name = "EducationDto")
public record EducationDto(
        UUID id,
        @NotNull(message = "The degree field is required.") String degree,
        @NotNull(message = "The fieldOfStudy field is required.") String fieldOfStudy,
        @NotNull(message = "The school field is required.") String school,
        String description) {
}
