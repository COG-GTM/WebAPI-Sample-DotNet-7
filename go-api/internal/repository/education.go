package repository

import (
	"context"
	"database/sql"
	"fmt"

	"github.com/COG-GTM/education-api/internal/domain"
	"github.com/google/uuid"
)

// EducationRepository defines the data-access contract mirroring
// src/Domain/Interfaces/IEducationRepository.cs.
type EducationRepository interface {
	GetAll(ctx context.Context) ([]domain.Education, error)
	GetByID(ctx context.Context, id uuid.UUID) (*domain.Education, error)
	Add(ctx context.Context, education domain.Education) (domain.Education, error)
	Update(ctx context.Context, education domain.Education) error
	Delete(ctx context.Context, id uuid.UUID) error
}

type postgresEducationRepository struct {
	db *sql.DB
}

// NewPostgresEducationRepository returns a PostgreSQL-backed EducationRepository.
func NewPostgresEducationRepository(db *sql.DB) EducationRepository {
	return &postgresEducationRepository{db: db}
}

func (r *postgresEducationRepository) GetAll(ctx context.Context) ([]domain.Education, error) {
	// TODO: implement – SELECT * FROM educations
	return nil, fmt.Errorf("not implemented")
}

func (r *postgresEducationRepository) GetByID(ctx context.Context, id uuid.UUID) (*domain.Education, error) {
	// TODO: implement – SELECT … WHERE id = $1
	return nil, fmt.Errorf("not implemented")
}

func (r *postgresEducationRepository) Add(ctx context.Context, education domain.Education) (domain.Education, error) {
	// TODO: implement – INSERT INTO educations …
	return domain.Education{}, fmt.Errorf("not implemented")
}

func (r *postgresEducationRepository) Update(ctx context.Context, education domain.Education) error {
	// TODO: implement – UPDATE educations SET … WHERE id = $1
	return fmt.Errorf("not implemented")
}

func (r *postgresEducationRepository) Delete(ctx context.Context, id uuid.UUID) error {
	// TODO: implement – DELETE FROM educations WHERE id = $1
	return fmt.Errorf("not implemented")
}
