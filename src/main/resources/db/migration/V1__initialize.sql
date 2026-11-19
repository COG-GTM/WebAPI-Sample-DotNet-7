-- Same schema and seed row as the former EF Core migration 20240113141226_Initialize.
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
