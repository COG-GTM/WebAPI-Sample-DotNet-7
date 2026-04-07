CREATE TABLE IF NOT EXISTS educations (
    id UUID PRIMARY KEY,
    degree VARCHAR(50) NOT NULL,
    field_of_study VARCHAR(250) NOT NULL,
    school VARCHAR(250) NOT NULL,
    description VARCHAR(1000)
);
