package domain

import "github.com/google/uuid"

// Education represents the core domain entity mirroring src/Domain/Entities/Education.cs.
type Education struct {
	ID           uuid.UUID
	Degree       string
	FieldOfStudy string
	School       string
	Description  string
}
