package com.sample.webapi.web;

import static org.hamcrest.Matchers.hasSize;
import static org.hamcrest.Matchers.notNullValue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.sample.webapi.application.EducationDto;
import com.sample.webapi.application.EducationService;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.webmvc.test.autoconfigure.WebMvcTest;
import org.springframework.http.MediaType;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

@WebMvcTest(EducationsController.class)
class EducationsControllerTest {

    private static final String BASE = "/api/Educations";
    private static final String VALID_BODY = """
            {"degree":"Bachelor's degree","fieldOfStudy":"Software engineering","school":"Sample university"}""";

    @Autowired
    private MockMvc mvc;

    @MockitoBean
    private EducationService educationService;

    private static EducationDto dto(UUID id) {
        return new EducationDto(id, "Bachelor's degree", "Software engineering", "Sample university", null);
    }

    private static String bodyWithId(UUID id) {
        return """
                {"id":"%s","degree":"PhD","fieldOfStudy":"Physics","school":"Sample university","description":null}"""
                .formatted(id);
    }

    // GET

    @Test
    void get_withData_returnsOk() throws Exception {
        when(educationService.getAll()).thenReturn(List.of(dto(UUID.randomUUID()), dto(UUID.randomUUID()), dto(UUID.randomUUID())));

        mvc.perform(get(BASE))
                .andExpect(status().isOk())
                .andExpect(content().contentType(MediaType.APPLICATION_JSON))
                .andExpect(jsonPath("$", hasSize(3)))
                .andExpect(jsonPath("$[0].description").value(org.hamcrest.Matchers.nullValue()));
    }

    @Test
    void get_routeIsCaseInsensitive() throws Exception {
        when(educationService.getAll()).thenReturn(List.of());

        mvc.perform(get("/api/educations")).andExpect(status().isOk());
        mvc.perform(get("/API/EDUCATIONS")).andExpect(status().isOk());
    }

    @Test
    void get_onException_returnsInternalServerError() throws Exception {
        when(educationService.getAll()).thenThrow(new RuntimeException("boom"));

        mvc.perform(get(BASE))
                .andExpect(status().isInternalServerError())
                .andExpect(content().contentType(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.status").value(500))
                .andExpect(jsonPath("$.title").value("An error occurred while processing your request."));
    }

    @Test
    void getById_withValidId_returnsOk() throws Exception {
        UUID id = UUID.randomUUID();
        when(educationService.getById(id)).thenReturn(Optional.of(dto(id)));

        mvc.perform(get(BASE + "/" + id))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(id.toString()));
    }

    @Test
    void getById_withUnknownId_returnsNoContent() throws Exception {
        UUID id = UUID.randomUUID();
        when(educationService.getById(id)).thenReturn(Optional.empty());

        mvc.perform(get(BASE + "/" + id))
                .andExpect(status().isNoContent())
                .andExpect(content().string(""));
    }

    @Test
    void getById_withNonGuidId_returnsNotFound() throws Exception {
        mvc.perform(get(BASE + "/not-a-guid"))
                .andExpect(status().isNotFound())
                .andExpect(content().string(""));
    }

    @Test
    void getById_onException_returnsInternalServerError() throws Exception {
        UUID id = UUID.randomUUID();
        when(educationService.getById(id)).thenThrow(new RuntimeException());

        mvc.perform(get(BASE + "/" + id)).andExpect(status().isInternalServerError());
    }

    // POST

    @Test
    void post_withValidModel_returnsOk() throws Exception {
        UUID newId = UUID.randomUUID();
        when(educationService.add(any())).thenReturn(dto(newId));

        mvc.perform(post(BASE).contentType(MediaType.APPLICATION_JSON).content(VALID_BODY))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(newId.toString()));
    }

    @Test
    void post_withEmptyStringField_isAccepted() throws Exception {
        when(educationService.add(any())).thenReturn(dto(UUID.randomUUID()));

        mvc.perform(post(BASE).contentType(MediaType.APPLICATION_JSON)
                        .content("{\"degree\":\"\",\"fieldOfStudy\":\"f\",\"school\":\"s\"}"))
                .andExpect(status().isOk());
    }

    @Test
    void post_withNullInput_returnsBadRequest() throws Exception {
        mvc.perform(post(BASE).contentType(MediaType.APPLICATION_JSON).content(""))
                .andExpect(status().isBadRequest())
                .andExpect(content().contentType(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.title").value("One or more validation errors occurred."))
                .andExpect(jsonPath("$.errors.body[0]").value("A non-empty request body is required."));
        verify(educationService, never()).add(any());
    }

    @Test
    void post_withMissingRequiredField_returnsValidationProblem() throws Exception {
        mvc.perform(post(BASE).contentType(MediaType.APPLICATION_JSON)
                        .content("{\"fieldOfStudy\":\"f\",\"school\":null}"))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.type").value("https://tools.ietf.org/html/rfc7231#section-6.5.1"))
                .andExpect(jsonPath("$.traceId").value(notNullValue()))
                .andExpect(jsonPath("$.errors.degree[0]").value("The degree field is required."))
                .andExpect(jsonPath("$.errors.school[0]").value("The school field is required."));
        verify(educationService, never()).add(any());
    }

    @Test
    void post_withMalformedJson_returnsBadRequest() throws Exception {
        mvc.perform(post(BASE).contentType(MediaType.APPLICATION_JSON).content("{not json"))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.errors.body[0]").value("The request body is not valid JSON."));
    }

    @Test
    void post_withUnsupportedMediaType_returns415() throws Exception {
        mvc.perform(post(BASE).contentType(MediaType.TEXT_PLAIN).content("hello"))
                .andExpect(status().isUnsupportedMediaType())
                .andExpect(content().contentType(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.title").value("Unsupported Media Type"));
    }

    @Test
    void post_onException_returnsInternalServerError() throws Exception {
        when(educationService.add(any())).thenThrow(new RuntimeException());

        mvc.perform(post(BASE).contentType(MediaType.APPLICATION_JSON).content(VALID_BODY))
                .andExpect(status().isInternalServerError());
    }

    // PUT

    @Test
    void put_forSuccessfulUpdate_returnsOk() throws Exception {
        UUID id = UUID.randomUUID();
        when(educationService.update(eq(id), any())).thenReturn(true);

        mvc.perform(put(BASE + "/" + id).contentType(MediaType.APPLICATION_JSON).content(bodyWithId(id)))
                .andExpect(status().isOk())
                .andExpect(content().string(""));
    }

    @Test
    void put_forFailedUpdate_returnsBadRequest() throws Exception {
        UUID id = UUID.randomUUID();
        when(educationService.update(eq(id), any())).thenReturn(false);

        mvc.perform(put(BASE + "/" + id).contentType(MediaType.APPLICATION_JSON).content(bodyWithId(id)))
                .andExpect(status().isBadRequest())
                .andExpect(content().contentType(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.title").value("Bad Request"));
    }

    @Test
    void put_withNullInput_returnsBadRequest() throws Exception {
        mvc.perform(put(BASE + "/" + UUID.randomUUID()).contentType(MediaType.APPLICATION_JSON).content(""))
                .andExpect(status().isBadRequest());
        verify(educationService, never()).update(any(), any());
    }

    @Test
    void put_withNonGuidId_returnsNotFound() throws Exception {
        mvc.perform(put(BASE + "/not-a-guid").contentType(MediaType.APPLICATION_JSON).content(bodyWithId(UUID.randomUUID())))
                .andExpect(status().isNotFound());
    }

    @Test
    void put_onException_returnsInternalServerError() throws Exception {
        UUID id = UUID.randomUUID();
        when(educationService.update(any(), any())).thenThrow(new RuntimeException());

        mvc.perform(put(BASE + "/" + id).contentType(MediaType.APPLICATION_JSON).content(bodyWithId(id)))
                .andExpect(status().isInternalServerError());
    }

    // DELETE

    @Test
    void delete_forSuccessfulDeletion_returnsOk() throws Exception {
        when(educationService.delete(any())).thenReturn(true);

        mvc.perform(delete(BASE + "/" + UUID.randomUUID()))
                .andExpect(status().isOk())
                .andExpect(content().string(""));
    }

    @Test
    void delete_forFailedDeletion_returnsBadRequest() throws Exception {
        when(educationService.delete(any())).thenReturn(false);

        mvc.perform(delete(BASE + "/" + UUID.randomUUID())).andExpect(status().isBadRequest());
    }

    @Test
    void delete_onException_returnsInternalServerError() throws Exception {
        when(educationService.delete(any())).thenThrow(new RuntimeException());

        mvc.perform(delete(BASE + "/" + UUID.randomUUID())).andExpect(status().isInternalServerError());
    }

    @Test
    void unsupportedMethod_returnsMethodNotAllowed() throws Exception {
        mvc.perform(org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch(BASE))
                .andExpect(status().isMethodNotAllowed());
    }
}
