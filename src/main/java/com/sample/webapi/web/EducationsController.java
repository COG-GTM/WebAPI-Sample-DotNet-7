package com.sample.webapi.web;

import com.sample.webapi.application.EducationDto;
import com.sample.webapi.application.EducationService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import java.util.List;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * CRUD endpoints for education records. Paths are matched case-insensitively (see {@code WebConfig});
 * unexpected errors are mapped to HTTP responses by {@link ApiExceptionHandler}.
 */
@RestController
@RequestMapping("/api/Educations")
@Tag(name = "Educations")
public class EducationsController {

    private final EducationService educationService;

    public EducationsController(EducationService educationService) {
        this.educationService = educationService;
    }

    @Operation(operationId = "listEducations", summary = "List all education records")
    @GetMapping
    public ResponseEntity<List<EducationDto>> getAll() {
        return ResponseEntity.ok(educationService.getAll());
    }

    @Operation(operationId = "getEducation", summary = "Get an education record by id (204 if it does not exist)")
    @GetMapping("/{id}")
    public ResponseEntity<EducationDto> getById(@PathVariable UUID id) {
        return educationService.getById(id)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.noContent().build());
    }

    @Operation(operationId = "createEducation", summary = "Create an education record")
    @PostMapping
    public ResponseEntity<Object> create(@Valid @RequestBody EducationDto model) {
        if (model == null) {
            return ProblemResponses.of(HttpStatus.BAD_REQUEST);
        }
        return ResponseEntity.ok(educationService.add(model));
    }

    @Operation(operationId = "updateEducation", summary = "Replace an education record (body id must match the path id)")
    @PutMapping("/{id}")
    public ResponseEntity<Object> update(@PathVariable UUID id, @Valid @RequestBody EducationDto model) {
        return educationService.update(id, model)
                ? ResponseEntity.ok().build()
                : ProblemResponses.of(HttpStatus.BAD_REQUEST);
    }

    @Operation(operationId = "deleteEducation", summary = "Delete an education record")
    @DeleteMapping("/{id}")
    public ResponseEntity<Object> delete(@PathVariable UUID id) {
        return educationService.delete(id)
                ? ResponseEntity.ok().build()
                : ProblemResponses.of(HttpStatus.BAD_REQUEST);
    }
}
