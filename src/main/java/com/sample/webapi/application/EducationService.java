package com.sample.webapi.application;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface EducationService {

    List<EducationDto> getAll();

    Optional<EducationDto> getById(UUID id);

    /** Creates a record with a newly generated id; any id in {@code model} is ignored. */
    EducationDto add(EducationDto model);

    /** Replaces the record; returns {@code false} if {@code model.id()} differs from {@code id} or no record exists. */
    boolean update(UUID id, EducationDto model);

    /** Returns {@code false} if no record exists for {@code id}. */
    boolean delete(UUID id);
}
