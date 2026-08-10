using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.DbContexts
{
    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

        public DbSet<Education> Educations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Seed: the id is fixed so the model is stable across builds,
            // otherwise EF Core reports pending model changes on every migration command.
            modelBuilder.Entity<Education>().HasData(
                new Education
                {
                    Id = new Guid("c92ea179-dd5c-46ca-b7b5-b44a191b974c"),
                    Degree = "Bachelor's degree",
                    FieldOfStudy = "Software engineering",
                    School = "Sample university"
                }
                );
        }
    }
}