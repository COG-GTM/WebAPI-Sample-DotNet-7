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

            // Seed
            modelBuilder.Entity<Education>().HasData(
                new Education
                {
                    Id = Guid.Parse("c92ea179-5be9-4a80-8991-0e2679c83b9c"),
                    Degree = "Bachelor's degree",
                    FieldOfStudy = "Software engineering",
                    School = "Sample university"
                }
                );
        }
    }
}
