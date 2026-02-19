using Domain.Entities;
using Infrastructure.DbContexts;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests
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
        public async Task GetAll_EmptyDatabase_ReturnsEmptyCollection()
        {
            var result = await _repository.GetAll();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAll_WithMultipleRecords_ReturnsAllRecords()
        {
            var education1 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "Computer Science",
                School = "MIT"
            };
            var education2 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's",
                FieldOfStudy = "Data Science",
                School = "Stanford"
            };
            await _dbContext.Educations.AddRangeAsync(education1, education2);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetAll();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAll_ReturnsCorrectData()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "PhD",
                FieldOfStudy = "Physics",
                School = "Caltech",
                Description = "Quantum mechanics research"
            };
            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();

            var result = (await _repository.GetAll()).ToList();

            Assert.Single(result);
            Assert.Equal(education.Id, result[0].Id);
            Assert.Equal("PhD", result[0].Degree);
            Assert.Equal("Physics", result[0].FieldOfStudy);
            Assert.Equal("Caltech", result[0].School);
            Assert.Equal("Quantum mechanics research", result[0].Description);
        }

        [Fact]
        public async Task GetById_ExistingId_ReturnsCorrectRecord()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Mathematics",
                School = "Harvard"
            };
            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal("Bachelor's", result.Degree);
            Assert.Equal("Mathematics", result.FieldOfStudy);
            Assert.Equal("Harvard", result.School);
        }

        [Fact]
        public async Task GetById_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetById_WithMultipleRecords_ReturnsCorrectOne()
        {
            var targetId = Guid.NewGuid();
            var education1 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "Arts",
                School = "School A"
            };
            var education2 = new Education
            {
                Id = targetId,
                Degree = "Master's",
                FieldOfStudy = "Science",
                School = "School B"
            };
            await _dbContext.Educations.AddRangeAsync(education1, education2);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetById(targetId);

            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
            Assert.Equal("Master's", result.Degree);
            Assert.Equal("School B", result.School);
        }

        [Fact]
        public async Task GetById_ReturnsUntrackedEntity()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "General Studies",
                School = "Test University"
            };
            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetById(id);

            Assert.NotNull(result);
            var entry = _dbContext.Entry(result);
            Assert.Equal(EntityState.Detached, entry.State);
        }

        [Fact]
        public async Task Add_ValidEducation_ReturnsAddedEntity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Associate's",
                FieldOfStudy = "Nursing",
                School = "Community College",
                Description = "Healthcare program"
            };

            var result = await _repository.Add(education);

            Assert.NotNull(result);
            Assert.Equal(education.Id, result.Id);
            Assert.Equal("Associate's", result.Degree);
            Assert.Equal("Nursing", result.FieldOfStudy);
            Assert.Equal("Community College", result.School);
            Assert.Equal("Healthcare program", result.Description);
        }

        [Fact]
        public async Task Add_PersistsAfterSaveChanges()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "Engineering",
                School = "Georgia Tech"
            };

            await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            var saved = await _dbContext.Educations.FindAsync(id);
            Assert.NotNull(saved);
            Assert.Equal("Master's", saved.Degree);
            Assert.Equal("Engineering", saved.FieldOfStudy);
            Assert.Equal("Georgia Tech", saved.School);
        }

        [Fact]
        public async Task Add_WithNullDescription_Succeeds()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "Computer Science",
                School = "Test University",
                Description = null
            };

            var result = await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            Assert.NotNull(result);
            var saved = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(saved);
            Assert.Equal("Bachelor's", saved.Degree);
            Assert.Equal("Computer Science", saved.FieldOfStudy);
            Assert.Equal("Test University", saved.School);
            Assert.Null(saved.Description);
        }

        [Fact]
        public async Task Update_ModifiesExistingRecord()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Biology",
                School = "UCLA"
            };
            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(education).State = EntityState.Detached;

            var updatedEducation = new Education
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "Molecular Biology",
                School = "UCLA",
                Description = "Graduate program"
            };

            _repository.Update(updatedEducation);
            await _dbContext.SaveChangesAsync();

            var result = await _dbContext.Educations.AsNoTracking().FirstAsync(e => e.Id == id);
            Assert.Equal("Master's", result.Degree);
            Assert.Equal("Molecular Biology", result.FieldOfStudy);
            Assert.Equal("UCLA", result.School);
            Assert.Equal("Graduate program", result.Description);
        }

        [Fact]
        public async Task Update_MarksEntityAsModified()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Arts",
                School = "Test School"
            };
            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(education).State = EntityState.Detached;

            var updatedEducation = new Education
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "Fine Arts",
                School = "Updated School"
            };

            _repository.Update(updatedEducation);

            var entry = _dbContext.Entry(updatedEducation);
            Assert.Equal(EntityState.Modified, entry.State);
        }

        [Fact]
        public async Task Delete_RemovesRecord()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "PhD",
                FieldOfStudy = "Chemistry",
                School = "Oxford"
            };
            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education);
            await _dbContext.SaveChangesAsync();

            var result = await _dbContext.Educations.FindAsync(id);
            Assert.Null(result);
        }

        [Fact]
        public async Task Delete_MarksEntityAsDeleted()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "History",
                School = "Test School"
            };
            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education);

            var entry = _dbContext.Entry(education);
            Assert.Equal(EntityState.Deleted, entry.State);
        }

        [Fact]
        public async Task Delete_OnlyRemovesTargetRecord()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var education1 = new Education { Id = id1, Degree = "Bachelor's", FieldOfStudy = "Arts", School = "School A" };
            var education2 = new Education { Id = id2, Degree = "Master's", FieldOfStudy = "Science", School = "School B" };
            await _dbContext.Educations.AddRangeAsync(education1, education2);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education1);
            await _dbContext.SaveChangesAsync();

            var remaining = await _dbContext.Educations.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(id2, remaining[0].Id);
        }

        [Fact]
        public async Task Add_ThenGetById_ReturnsConsistentData()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "MBA",
                FieldOfStudy = "Business Administration",
                School = "Wharton",
                Description = "Executive program"
            };

            await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal("MBA", result.Degree);
            Assert.Equal("Business Administration", result.FieldOfStudy);
            Assert.Equal("Wharton", result.School);
            Assert.Equal("Executive program", result.Description);
        }

        [Fact]
        public async Task Add_ThenDelete_ThenGetAll_ReturnsEmpty()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "General Studies",
                School = "Test University"
            };

            await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetAll();
            Assert.Empty(result);
        }

        [Fact]
        public async Task Add_MultipleThenGetAll_ReturnsAll()
        {
            for (int i = 0; i < 5; i++)
            {
                await _repository.Add(new Education
                {
                    Id = Guid.NewGuid(),
                    Degree = $"Degree {i}",
                    FieldOfStudy = $"Field {i}",
                    School = $"School {i}"
                });
            }
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetAll();
            Assert.Equal(5, result.Count());
        }
    }
}
