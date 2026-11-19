# syntax=docker/dockerfile:1

FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src
COPY pom.xml .
COPY src ./src
# Optional: --secret id=maven_settings,src=$HOME/.m2/settings.xml to use a Maven mirror.
RUN --mount=type=cache,target=/root/.m2 \
    --mount=type=secret,id=maven_settings,target=/run/secrets/maven_settings,required=false \
    MVN_SETTINGS=""; [ -s /run/secrets/maven_settings ] && MVN_SETTINGS="-s /run/secrets/maven_settings"; \
    mvn -B -q $MVN_SETTINGS package -DskipTests && cp target/webapi-sample-*.jar /app.jar

FROM eclipse-temurin:21-jre
RUN groupadd --system app && useradd --system --gid app app
WORKDIR /app
COPY --from=build /app.jar app.jar
USER app
EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s \
    CMD ["sh", "-c", "exec 3<>/dev/tcp/127.0.0.1/5000 && printf 'GET /actuator/health/readiness HTTP/1.0\\r\\n\\r\\n' >&3 && grep -q UP <&3"]
ENTRYPOINT ["java", "-XX:MaxRAMPercentage=75", "-jar", "app.jar"]
