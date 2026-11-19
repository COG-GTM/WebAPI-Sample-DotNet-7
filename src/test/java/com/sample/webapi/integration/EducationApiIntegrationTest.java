package com.sample.webapi.integration;

import static org.hamcrest.Matchers.hasItem;
import static org.hamcrest.Matchers.not;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.jayway.jsonpath.JsonPath;
import com.sample.webapi.PostgresContainerSupport;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;

@SpringBootTest
@AutoConfigureMockMvc
class EducationApiIntegrationTest extends PostgresContainerSupport {

    private static final String SEED_ID = "c92ea179-dd5c-46ca-b7b5-b44a191b974c";

    @Autowired
    private MockMvc mvc;

    @Test
    void seedRowFromMigrationIsReturned() throws Exception {
        mvc.perform(get("/api/Educations/" + SEED_ID))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.degree").value("Bachelor's degree"))
                .andExpect(jsonPath("$.fieldOfStudy").value("Software engineering"))
                .andExpect(jsonPath("$.school").value("Sample university"));
    }

    @Test
    void createReadUpdateDelete() throws Exception {
        String created = mvc.perform(post("/api/Educations").contentType(MediaType.APPLICATION_JSON).content("""
                        {"id":"%s","degree":"MSc","fieldOfStudy":"Data science","school":"Test university","description":"d"}"""
                        .formatted(SEED_ID)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(not(SEED_ID)))
                .andReturn().getResponse().getContentAsString();
        String id = JsonPath.read(created, "$.id");

        mvc.perform(get("/api/educations")).andExpect(jsonPath("$[*].id", hasItem(id)));

        mvc.perform(put("/api/Educations/" + id).contentType(MediaType.APPLICATION_JSON).content("""
                        {"id":"%s","degree":"PhD","fieldOfStudy":"Data science","school":"Test university"}""".formatted(id)))
                .andExpect(status().isOk());
        mvc.perform(get("/api/Educations/" + id))
                .andExpect(jsonPath("$.degree").value("PhD"))
                .andExpect(jsonPath("$.description").doesNotExist());

        mvc.perform(delete("/api/Educations/" + id)).andExpect(status().isOk());
        mvc.perform(get("/api/Educations/" + id)).andExpect(status().isNoContent());
        mvc.perform(delete("/api/Educations/" + id)).andExpect(status().isBadRequest());
    }

    @Test
    void updateOfMissingRecordReturnsBadRequest() throws Exception {
        String missing = "00000000-0000-4000-8000-000000000001";
        mvc.perform(put("/api/Educations/" + missing).contentType(MediaType.APPLICATION_JSON).content("""
                        {"id":"%s","degree":"PhD","fieldOfStudy":"f","school":"s"}""".formatted(missing)))
                .andExpect(status().isBadRequest());
    }

    @Test
    void databaseConstraintViolationReturnsInternalServerError() throws Exception {
        mvc.perform(post("/api/Educations").contentType(MediaType.APPLICATION_JSON).content("""
                        {"degree":"%s","fieldOfStudy":"f","school":"s"}""".formatted("x".repeat(51))))
                .andExpect(status().isInternalServerError())
                .andExpect(jsonPath("$.status").value(500));
    }

    @Test
    void healthEndpointIsUp() throws Exception {
        mvc.perform(get("/actuator/health")).andExpect(status().isOk()).andExpect(jsonPath("$.status").value("UP"));
    }
}
