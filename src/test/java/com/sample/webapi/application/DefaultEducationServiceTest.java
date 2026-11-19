package com.sample.webapi.application;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.sample.webapi.domain.Education;
import com.sample.webapi.domain.EducationRepository;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class DefaultEducationServiceTest {

    @Mock
    private EducationRepository repository;

    @InjectMocks
    private DefaultEducationService service;

    private static Education entity(UUID id) {
        Education e = new Education();
        e.setId(id);
        e.setDegree("Bachelor's degree");
        e.setFieldOfStudy("Software engineering");
        e.setSchool("Sample university");
        return e;
    }

    private static EducationDto dto(UUID id) {
        return new EducationDto(id, "Master's degree", "Computer science", "Sample university", "desc");
    }

    @Test
    void getAll_withData_returnsListOfEducationDto() {
        when(repository.findAll()).thenReturn(List.of(entity(UUID.randomUUID()), entity(UUID.randomUUID())));

        List<EducationDto> result = service.getAll();

        assertThat(result).hasSize(2).allSatisfy(d -> assertThat(d.degree()).isEqualTo("Bachelor's degree"));
    }

    @Test
    void getAll_withoutData_returnsEmptyList() {
        when(repository.findAll()).thenReturn(List.of());

        assertThat(service.getAll()).isEmpty();
    }

    @Test
    void getById_withData_returnsEducationDto() {
        UUID id = UUID.randomUUID();
        when(repository.findById(id)).thenReturn(Optional.of(entity(id)));

        assertThat(service.getById(id)).get().extracting(EducationDto::id).isEqualTo(id);
    }

    @Test
    void getById_missing_returnsEmpty() {
        UUID id = UUID.randomUUID();
        when(repository.findById(id)).thenReturn(Optional.empty());

        assertThat(service.getById(id)).isEmpty();
    }

    @Test
    void add_generatesNewId_andReturnsAddedEducationDto() {
        UUID clientId = UUID.randomUUID();
        when(repository.saveAndFlush(any(Education.class))).thenAnswer(inv -> inv.getArgument(0));

        EducationDto result = service.add(dto(clientId));

        ArgumentCaptor<Education> saved = ArgumentCaptor.forClass(Education.class);
        verify(repository).saveAndFlush(saved.capture());
        assertThat(saved.getValue().getId()).isNotNull().isNotEqualTo(clientId);
        assertThat(result.id()).isEqualTo(saved.getValue().getId());
        assertThat(result.degree()).isEqualTo("Master's degree");
    }

    @Test
    void update_forSuccessfulUpdate_returnsTrue() {
        UUID id = UUID.randomUUID();
        when(repository.existsById(id)).thenReturn(true);

        boolean result = service.update(id, dto(id));

        assertThat(result).isTrue();
        ArgumentCaptor<Education> saved = ArgumentCaptor.forClass(Education.class);
        verify(repository).saveAndFlush(saved.capture());
        assertThat(saved.getValue().getId()).isEqualTo(id);
        assertThat(saved.getValue().getDescription()).isEqualTo("desc");
    }

    @Test
    void update_forIdMismatch_returnsFalse() {
        boolean result = service.update(UUID.randomUUID(), dto(UUID.randomUUID()));

        assertThat(result).isFalse();
        verify(repository, never()).existsById(any());
        verify(repository, never()).saveAndFlush(any());
    }

    @Test
    void update_forNullModel_returnsFalse() {
        assertThat(service.update(UUID.randomUUID(), null)).isFalse();
        verify(repository, never()).saveAndFlush(any());
    }

    @Test
    void update_forMissingRecord_returnsFalse() {
        UUID id = UUID.randomUUID();
        when(repository.existsById(id)).thenReturn(false);

        assertThat(service.update(id, dto(id))).isFalse();
        verify(repository, never()).saveAndFlush(any());
    }

    @Test
    void delete_withExistingId_returnsTrue() {
        UUID id = UUID.randomUUID();
        Education existing = entity(id);
        when(repository.findById(id)).thenReturn(Optional.of(existing));

        assertThat(service.delete(id)).isTrue();
        verify(repository).delete(existing);
        verify(repository).flush();
    }

    @Test
    void delete_withMissingId_returnsFalse() {
        UUID id = UUID.randomUUID();
        when(repository.findById(id)).thenReturn(Optional.empty());

        assertThat(service.delete(id)).isFalse();
        verify(repository, never()).delete(any());
    }
}
