package com.sample.webapi.config;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Info;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class OpenApiConfig {

    @Bean
    OpenAPI webApiOpenApi() {
        return new OpenAPI().info(new Info().title("WebApi").version("v1")
                .description("Educations API. Source of truth: api-spec/openapi.yaml"));
    }
}
