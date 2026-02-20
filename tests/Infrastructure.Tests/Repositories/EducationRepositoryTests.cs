using Domain.Entities;
using Infrastructure.DbContexts;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Repositories
{
    public class EducationRepositoryTests : IDisposable
    {
        private readonly SampleDbContext _dbContext;
        private readonly EducationRepository _repository;

        public EducationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new SampleDbContext(options);
            _repository = new EducationRepository(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetAll_WhenNoData_ReturnsEmptyList()
        {
            var result = await _repository.GetAll();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAll_WhenDataExists_ReturnsAllEducations()
        {
            var education1 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "CS",
                School = "MIT"
            };
            var education2 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's",
                FieldOfStudy = "AI",
                School = "Stanford"
            };
            _dbContext.Educations.AddRange(education1, education2);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetAll();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetById_WhenExists_ReturnsEducation()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "PhD",
                FieldOfStudy = "Physics",
                School = "Caltech"
            };
            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.Id);
            Assert.Equal("PhD", result.Degree);
            Assert.Equal("Physics", result.FieldOfStudy);
            Assert.Equal("Caltech", result.School);
        }

        [Fact]
        public async Task GetById_WhenNotExists_ReturnsNull()
        {
            var result = await _repository.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetById_UsesAsNoTracking()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Math",
                School = "Harvard"
            };
            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            var result = await _repository.GetById(id);

            Assert.NotNull(result);
            var entry = _dbContext.Entry(result!);
            Assert.Equal(EntityState.Detached, entry.State);
        }

        [Fact]
        public async Task Add_ReturnsAddedEntity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Associate's",
                FieldOfStudy = "Business",
                School = "Community College"
            };

            var result = await _repository.Add(education);

            Assert.NotNull(result);
            Assert.Equal(education.Id, result.Id);
            Assert.Equal("Associate's", result.Degree);
            Assert.Equal("Business", result.FieldOfStudy);
            Assert.Equal("Community College", result.School);
        }

        [Fact]
        public async Task Add_EntityIsPersisted_AfterSaveChanges()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "MBA",
                FieldOfStudy = "Finance",
                School = "Wharton"
            };

            await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            var persisted = await _dbContext.Educations.FindAsync(id);
            Assert.NotNull(persisted);
            Assert.Equal("MBA", persisted!.Degree);
        }

        [Fact]
        public async Task Update_ModifiesEntity()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Chemistry",
                School = "Yale"
            };
            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            var updated = new Education
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "Organic Chemistry",
                School = "Yale",
                Description = "Updated description"
            };

            _repository.Update(updated);
            await _dbContext.SaveChangesAsync();

            var result = await _dbContext.Educations.FindAsync(id);
            Assert.NotNull(result);
            Assert.Equal("Master's", result!.Degree);
            Assert.Equal("Organic Chemistry", result.FieldOfStudy);
            Assert.Equal("Updated description", result.Description);
        }

        [Fact]
        public async Task Delete_RemovesEntity()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Doctorate",
                FieldOfStudy = "Medicine",
                School = "Johns Hopkins"
            };
            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education);
            await _dbContext.SaveChangesAsync();

            var result = await _dbContext.Educations.FindAsync(id);
            Assert.Null(result);
        }

        [Fact]
        public async Task Delete_OnlyRemovesSpecifiedEntity()
        {
            var education1 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "English",
                School = "Oxford"
            };
            var education2 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's",
                FieldOfStudy = "History",
                School = "Cambridge"
            };
            _dbContext.Educations.AddRange(education1, education2);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education1);
            await _dbContext.SaveChangesAsync();

            var remaining = await _dbContext.Educations.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(education2.Id, remaining[0].Id);
        }

        [Fact]
        public async Task GetAll_ReturnsCorrectProperties()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "Art",
                School = "RISD",
                Description = "Fine arts program"
            };
            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            var result = (await _repository.GetAll()).First();

            Assert.Equal(education.Id, result.Id);
            Assert.Equal("Bachelor's", result.Degree);
            Assert.Equal("Art", result.FieldOfStudy);
            Assert.Equal("RISD", result.School);
            Assert.Equal("Fine arts program", result.Description);
        }

        [Fact]
        public async Task Add_WithNullDescription_Succeeds()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "CS",
                School = "MIT",
                Description = null
            };

            var result = await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            Assert.NotNull(result);
            var persisted = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(persisted);
            Assert.Null(persisted!.Description);
        }

        [Fact]
        public async Task GetById_WithMultipleRecords_ReturnsCorrectOne()
        {
            var targetId = Guid.NewGuid();
            var educations = new List<Education>
            {
                new Education { Id = Guid.NewGuid(), Degree = "AA", FieldOfStudy = "Field A", School = "School A" },
                new Education { Id = targetId, Degree = "BB", FieldOfStudy = "Field B", School = "School B" },
                new Education { Id = Guid.NewGuid(), Degree = "CC", FieldOfStudy = "Field C", School = "School C" }
            };
            _dbContext.Educations.AddRange(educations);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetById(targetId);

            Assert.NotNull(result);
            Assert.Equal("BB", result!.Degree);
            Assert.Equal("School B", result.School);
        }
    }
}
