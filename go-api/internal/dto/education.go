package dto

import (
	"github.com/COG-GTM/education-api/internal/domain"
	"github.com/google/uuid"
)

// EducationDTO is the data-transfer object used for API serialization.
type EducationDTO struct {
	ID           string `json:"id"`
	Degree       string `json:"degree"`
	FieldOfStudy string `json:"fieldOfStudy"`
	School       string `json:"school"`
	Description  string `json:"description,omitempty"`
}

// ToDTO converts a domain Education entity to an EducationDTO.
func ToDTO(e domain.Education) EducationDTO {
	return EducationDTO{
		ID:           e.ID.String(),
		Degree:       e.Degree,
		FieldOfStudy: e.FieldOfStudy,
		School:       e.School,
		Description:  e.Description,
	}
}

// ToDomain converts an EducationDTO to a domain Education entity.
func ToDomain(d EducationDTO) domain.Education {
	id, _ := uuid.Parse(d.ID)
	return domain.Education{
		ID:           id,
		Degree:       d.Degree,
		FieldOfStudy: d.FieldOfStudy,
		School:       d.School,
		Description:  d.Description,
	}
}
