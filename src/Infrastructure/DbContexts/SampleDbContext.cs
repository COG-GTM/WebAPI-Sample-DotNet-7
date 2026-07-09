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

            // Seed a sample Education row. The Id must be a static, deterministic
            // value (not Guid.NewGuid()) so the model matches the migration snapshot;
            // EF Core 9 otherwise errors with PendingModelChangesWarning.
            modelBuilder.Entity<Education>().HasData(
                new Education
                {
                    Id = Guid.Parse("c92ea179-dd5c-46ca-b7b5-b44a191b974c"),
                    Degree = "Bachelor's degree",
                    FieldOfStudy = "Software engineering",
                    School = "Sample university"
                }
                );
        }
    }
}