package com.sample.webapi.contract;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;

import com.jayway.jsonpath.JsonPath;
import com.sample.webapi.PostgresContainerSupport;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.PathItem;
import io.swagger.v3.parser.OpenAPIV3Parser;
import io.swagger.v3.parser.core.models.ParseOptions;
import io.swagger.v3.parser.core.models.SwaggerParseResult;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.TreeSet;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.test.web.servlet.MockMvc;

/** Keeps api-spec/openapi.yaml valid and in sync with the implemented endpoints and payloads. */
@SpringBootTest(properties = "springdoc.api-docs.enabled=true")
@AutoConfigureMockMvc
class OpenApiContractTest extends PostgresContainerSupport {

    private static OpenAPI spec;

    @Autowired
    private MockMvc mvc;

    @BeforeAll
    static void parseSpec() {
        ParseOptions options = new ParseOptions();
        options.setResolve(true);
        SwaggerParseResult result = new OpenAPIV3Parser().readLocation("api-spec/openapi.yaml", null, options);
        assertThat(result.getMessages()).as("OpenAPI validation messages").isEmpty();
        spec = result.getOpenAPI();
    }

    @Test
    void specIsOpenApi3() {
        assertThat(spec.getOpenapi()).startsWith("3.");
    }

    @Test
    void specOperationsMatchImplementedOperations() throws Exception {
        String generated = mvc.perform(get("/v3/api-docs")).andReturn().getResponse().getContentAsString();
        Map<String, Map<String, Object>> generatedPaths = JsonPath.read(generated, "$.paths");

        Set<String> implemented = new TreeSet<>();
        generatedPaths.forEach((path, ops) -> ops.keySet().forEach(method -> implemented.add(method.toUpperCase() + " " + path)));

        Set<String> specified = new TreeSet<>();
        spec.getPaths().forEach((path, item) -> item.readOperationsMap().keySet()
                .forEach((PathItem.HttpMethod method) -> specified.add(method.name() + " " + path)));

        assertThat(implemented).isEqualTo(specified);
    }

    @Test
    void responseFieldsMatchEducationDtoSchema() throws Exception {
        String body = mvc.perform(get("/api/Educations")).andReturn().getResponse().getContentAsString();
        List<Map<String, Object>> items = JsonPath.read(body, "$");

        Set<String> schemaProperties = spec.getComponents().getSchemas().get("EducationDto").getProperties().keySet();
        assertThat(items).isNotEmpty().allSatisfy(item -> assertThat(item.keySet()).isEqualTo(schemaProperties));
    }

    @Test
    void problemResponseFieldsAreDeclaredInSchema() throws Exception {
        String body = mvc.perform(org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete("/api/Educations/00000000-0000-4000-8000-000000000001"))
                .andReturn().getResponse().getContentAsString();
        Map<String, Object> problem = JsonPath.read(body, "$");

        Set<String> schemaProperties = spec.getComponents().getSchemas().get("ProblemDetails").getProperties().keySet();
        assertThat(schemaProperties).containsAll(problem.keySet());
    }
}
