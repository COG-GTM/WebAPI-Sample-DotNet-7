package com.sample.webapi.application;

import com.sample.webapi.domain.Education;
import com.sample.webapi.domain.EducationRepository;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@Transactional
public class DefaultEducationService implements EducationService {

    private final EducationRepository repository;

    public DefaultEducationService(EducationRepository repository) {
        this.repository = repository;
    }

    @Override
    @Transactional(readOnly = true)
    public List<EducationDto> getAll() {
        return repository.findAll().stream().map(EducationMapper::toDto).toList();
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<EducationDto> getById(UUID id) {
        return repository.findById(id).map(EducationMapper::toDto);
    }

    @Override
    public EducationDto add(EducationDto model) {
        Education toAdd = EducationMapper.toEntity(model);
        toAdd.setId(UUID.randomUUID());
        Education saved = repository.saveAndFlush(toAdd);
        return EducationMapper.toDto(saved);
    }

    @Override
    public boolean update(UUID id, EducationDto model) {
        if (model == null || !id.equals(model.id())) {
            return false;
        }
        if (!repository.existsById(id)) {
            return false;
        }
        repository.saveAndFlush(EducationMapper.toEntity(model));
        return true;
    }

    @Override
    public boolean delete(UUID id) {
        Optional<Education> toDelete = repository.findById(id);
        if (toDelete.isEmpty()) {
            return false;
        }
        repository.delete(toDelete.get());
        repository.flush();
        return true;
    }
}
