CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Educations" (
    "Id" uuid NOT NULL,
    "Degree" character varying(50) NOT NULL,
    "FieldOfStudy" character varying(250) NOT NULL,
    "School" character varying(250) NOT NULL,
    "Description" character varying(1000) NULL,
    CONSTRAINT "PK_Educations" PRIMARY KEY ("Id")
);

INSERT INTO "Educations" ("Id", "Degree", "Description", "FieldOfStudy", "School")
VALUES ('c92ea179-dd5c-46ca-b7b5-b44a191b974c', 'Bachelor''s degree', NULL, 'Software engineering', 'Sample university');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240113141226_Initialize', '7.0.10');

COMMIT;

