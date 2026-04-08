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

            // Seed (use a fixed GUID to avoid EF Core PendingModelChangesWarning)
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
