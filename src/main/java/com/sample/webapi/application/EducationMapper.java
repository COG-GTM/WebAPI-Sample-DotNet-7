package com.sample.webapi.application;

import com.sample.webapi.domain.Education;

final class EducationMapper {

    private EducationMapper() {
    }

    static EducationDto toDto(Education entity) {
        return new EducationDto(entity.getId(), entity.getDegree(), entity.getFieldOfStudy(),
                entity.getSchool(), entity.getDescription());
    }

    static Education toEntity(EducationDto dto) {
        Education entity = new Education();
        entity.setId(dto.id());
        entity.setDegree(dto.degree());
        entity.setFieldOfStudy(dto.fieldOfStudy());
        entity.setSchool(dto.school());
        entity.setDescription(dto.description());
        return entity;
    }
}
