package handler

import (
	"bytes"
	"encoding/json"
	"net/http"

	"github.com/COG-GTM/education-api/internal/dto"
	"github.com/COG-GTM/education-api/internal/service"
	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"
)

// EducationHandler exposes HTTP endpoints mirroring
// src/WebApi/Controllers/EducationsController.cs.
type EducationHandler struct {
	service service.EducationService
}

// NewEducationHandler returns an EducationHandler wired to the given service.
func NewEducationHandler(svc service.EducationService) *EducationHandler {
	return &EducationHandler{service: svc}
}

// GetAll handles GET /api/educations – returns all education records.
func (h *EducationHandler) GetAll(w http.ResponseWriter, r *http.Request) {
	result, err := h.service.GetAll(r.Context())
	if err != nil {
		http.Error(w, http.StatusText(http.StatusInternalServerError), http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, result)
}

// GetByID handles GET /api/educations/{id} – returns a single education record.
func (h *EducationHandler) GetByID(w http.ResponseWriter, r *http.Request) {
	id, err := uuid.Parse(chi.URLParam(r, "id"))
	if err != nil {
		http.Error(w, "invalid id", http.StatusBadRequest)
		return
	}

	result, err := h.service.GetByID(r.Context(), id)
	if err != nil {
		http.Error(w, http.StatusText(http.StatusInternalServerError), http.StatusInternalServerError)
		return
	}
	if result == nil {
		w.WriteHeader(http.StatusNoContent)
		return
	}
	writeJSON(w, http.StatusOK, result)
}

// Create handles POST /api/educations – creates a new education record.
func (h *EducationHandler) Create(w http.ResponseWriter, r *http.Request) {
	var model dto.EducationDTO
	if err := json.NewDecoder(r.Body).Decode(&model); err != nil {
		http.Error(w, "invalid request body", http.StatusBadRequest)
		return
	}

	result, err := h.service.Add(r.Context(), model)
	if err != nil {
		http.Error(w, http.StatusText(http.StatusInternalServerError), http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, result)
}

// Update handles PUT /api/educations/{id} – updates an existing education record.
func (h *EducationHandler) Update(w http.ResponseWriter, r *http.Request) {
	id, err := uuid.Parse(chi.URLParam(r, "id"))
	if err != nil {
		http.Error(w, "invalid id", http.StatusBadRequest)
		return
	}

	var model dto.EducationDTO
	if err := json.NewDecoder(r.Body).Decode(&model); err != nil {
		http.Error(w, "invalid request body", http.StatusBadRequest)
		return
	}

	ok, err := h.service.Update(r.Context(), id, model)
	if err != nil {
		http.Error(w, http.StatusText(http.StatusInternalServerError), http.StatusInternalServerError)
		return
	}
	if !ok {
		http.Error(w, http.StatusText(http.StatusBadRequest), http.StatusBadRequest)
		return
	}
	w.WriteHeader(http.StatusOK)
}

// Delete handles DELETE /api/educations/{id} – removes an education record.
func (h *EducationHandler) Delete(w http.ResponseWriter, r *http.Request) {
	id, err := uuid.Parse(chi.URLParam(r, "id"))
	if err != nil {
		http.Error(w, "invalid id", http.StatusBadRequest)
		return
	}

	ok, err := h.service.Delete(r.Context(), id)
	if err != nil {
		http.Error(w, http.StatusText(http.StatusInternalServerError), http.StatusInternalServerError)
		return
	}
	if !ok {
		http.Error(w, http.StatusText(http.StatusBadRequest), http.StatusBadRequest)
		return
	}
	w.WriteHeader(http.StatusOK)
}

// writeJSON encodes v as JSON into a buffer before writing headers,
// so that encoding errors can still produce a proper 500 response.
func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	var buf bytes.Buffer
	if err := json.NewEncoder(&buf).Encode(v); err != nil {
		http.Error(w, http.StatusText(http.StatusInternalServerError), http.StatusInternalServerError)
		return
	}
	w.WriteHeader(status)
	_, _ = w.Write(buf.Bytes())
}
