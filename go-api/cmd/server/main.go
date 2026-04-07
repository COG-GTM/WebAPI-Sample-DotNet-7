package main

import (
	"database/sql"
	"fmt"
	"log"
	"net/http"
	"os"

	"github.com/COG-GTM/education-api/internal/handler"
	"github.com/COG-GTM/education-api/internal/repository"
	"github.com/COG-GTM/education-api/internal/service"
	"github.com/go-chi/chi/v5"
	"github.com/go-chi/chi/v5/middleware"
	_ "github.com/jackc/pgx/v5/stdlib" // registers "pgx" driver for database/sql
)

func main() {
	dsn := os.Getenv("DATABASE_URL")
	if dsn == "" {
		// Default matching the .NET project's Npgsql connection string
		// (see docker-compose.yml / appsettings.json for credentials).
		log.Fatal("DATABASE_URL environment variable is required")
	}

	db, err := sql.Open("pgx", dsn)
	if err != nil {
		log.Fatalf("failed to open database: %v", err)
	}
	defer db.Close()

	// Wire dependencies: Repository → Service → Handler.
	repo := repository.NewPostgresEducationRepository(db)
	svc := service.NewEducationService(repo)
	h := handler.NewEducationHandler(svc)

	r := chi.NewRouter()
	r.Use(middleware.Logger)
	r.Use(middleware.Recoverer)

	// Health-check endpoint that pings the database.
	r.Get("/health", func(w http.ResponseWriter, r *http.Request) {
		if err := db.PingContext(r.Context()); err != nil {
			http.Error(w, fmt.Sprintf("db ping failed: %v", err), http.StatusInternalServerError)
			return
		}
		w.WriteHeader(http.StatusOK)
		fmt.Fprintln(w, "ok")
	})

	// Education CRUD routes matching the .NET controller.
	r.Route("/api/educations", func(r chi.Router) {
		r.Get("/", h.GetAll)
		r.Post("/", h.Create)
		r.Get("/{id}", h.GetByID)
		r.Put("/{id}", h.Update)
		r.Delete("/{id}", h.Delete)
	})

	addr := ":8080"
	log.Printf("starting server on %s", addr)
	if err := http.ListenAndServe(addr, r); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
