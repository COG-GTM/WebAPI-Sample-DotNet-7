package service

import (
	"context"
	"fmt"

	"github.com/COG-GTM/education-api/internal/dto"
	"github.com/COG-GTM/education-api/internal/repository"
	"github.com/google/uuid"
)

// EducationService defines the application-level contract mirroring
// src/Application/Service/Interfaces/IEducationService.cs.
type EducationService interface {
	GetAll(ctx context.Context) ([]dto.EducationDTO, error)
	GetByID(ctx context.Context, id uuid.UUID) (*dto.EducationDTO, error)
	Add(ctx context.Context, model dto.EducationDTO) (dto.EducationDTO, error)
	Update(ctx context.Context, id uuid.UUID, model dto.EducationDTO) (bool, error)
	Delete(ctx context.Context, id uuid.UUID) (bool, error)
}

type educationService struct {
	repo repository.EducationRepository
}

// NewEducationService returns an EducationService backed by the given repository.
func NewEducationService(repo repository.EducationRepository) EducationService {
	return &educationService{repo: repo}
}

func (s *educationService) GetAll(ctx context.Context) ([]dto.EducationDTO, error) {
	educations, err := s.repo.GetAll(ctx)
	if err != nil {
		return nil, err
	}
	result := make([]dto.EducationDTO, 0, len(educations))
	for _, e := range educations {
		result = append(result, dto.ToDTO(e))
	}
	return result, nil
}

func (s *educationService) GetByID(ctx context.Context, id uuid.UUID) (*dto.EducationDTO, error) {
	education, err := s.repo.GetByID(ctx, id)
	if err != nil {
		return nil, err
	}
	if education == nil {
		return nil, nil
	}
	d := dto.ToDTO(*education)
	return &d, nil
}

// Add generates a new UUID for the entity, persists it via the repository,
// and returns the resulting DTO – mirroring EducationService.Add in .NET.
func (s *educationService) Add(ctx context.Context, model dto.EducationDTO) (dto.EducationDTO, error) {
	entity := dto.ToDomain(model)
	entity.ID = uuid.New()

	created, err := s.repo.Add(ctx, entity)
	if err != nil {
		return dto.EducationDTO{}, err
	}
	return dto.ToDTO(created), nil
}

// Update validates the request, fetches the existing entity, and updates it –
// mirroring EducationService.Update in .NET.
func (s *educationService) Update(ctx context.Context, id uuid.UUID, model dto.EducationDTO) (bool, error) {
	if model.ID != id.String() {
		return false, nil
	}

	existing, err := s.repo.GetByID(ctx, id)
	if err != nil {
		return false, err
	}
	if existing == nil {
		return false, nil
	}

	entity := dto.ToDomain(model)
	if err := s.repo.Update(ctx, entity); err != nil {
		return false, fmt.Errorf("update failed: %w", err)
	}
	return true, nil
}

// Delete fetches the entity and removes it – mirroring EducationService.Delete in .NET.
func (s *educationService) Delete(ctx context.Context, id uuid.UUID) (bool, error) {
	existing, err := s.repo.GetByID(ctx, id)
	if err != nil {
		return false, err
	}
	if existing == nil {
		return false, nil
	}

	if err := s.repo.Delete(ctx, id); err != nil {
		return false, fmt.Errorf("delete failed: %w", err)
	}
	return true, nil
}
