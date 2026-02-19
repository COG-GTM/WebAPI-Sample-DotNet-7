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

        #region GetAll

        [Fact]
        public async Task GetAll_WhenEmpty_ReturnsEmptyList()
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
                FieldOfStudy = "Software Engineering",
                School = "Stanford"
            };

            _dbContext.Educations.AddRange(education1, education2);
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
                FieldOfStudy = "Mathematics",
                School = "Oxford",
                Description = "Research focus"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            var result = (await _repository.GetAll()).ToList();

            Assert.Single(result);
            Assert.Equal(education.Id, result[0].Id);
            Assert.Equal("PhD", result[0].Degree);
            Assert.Equal("Mathematics", result[0].FieldOfStudy);
            Assert.Equal("Oxford", result[0].School);
            Assert.Equal("Research focus", result[0].Description);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_WithExistingId_ReturnsEducation()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Physics",
                School = "Caltech"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal("Bachelor's", result.Degree);
            Assert.Equal("Physics", result.FieldOfStudy);
            Assert.Equal("Caltech", result.School);
        }

        [Fact]
        public async Task GetById_WithNonExistingId_ReturnsNull()
        {
            var result = await _repository.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetById_ReturnsUntrackedEntity()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "Chemistry",
                School = "Harvard"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            var result = await _repository.GetById(id);

            Assert.NotNull(result);
            var entry = _dbContext.Entry(result);
            Assert.Equal(EntityState.Detached, entry.State);
        }

        #endregion

        #region Add

        [Fact]
        public async Task Add_WithValidModel_ReturnsAddedEntity()
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
        public async Task Add_PersistsAfterSaveChanges()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Diploma",
                FieldOfStudy = "Nursing",
                School = "Medical School"
            };

            await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            var persisted = await _dbContext.Educations.FindAsync(id);
            Assert.NotNull(persisted);
            Assert.Equal("Diploma", persisted.Degree);
        }

        [Fact]
        public async Task Add_WithNullDescription_Succeeds()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Engineering",
                School = "Tech University",
                Description = null
            };

            var result = await _repository.Add(education);
            await _dbContext.SaveChangesAsync();

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Null(result.Description);
        }

        [Fact]
        public async Task Add_SetsEntityStateToAdded()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's",
                FieldOfStudy = "Data Science",
                School = "Analytics Institute"
            };

            await _repository.Add(education);

            var entry = _dbContext.Entry(education);
            Assert.Equal(EntityState.Added, entry.State);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_ModifiesExistingEntity()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Art",
                School = "Art School"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            var updatedEducation = new Education
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "Fine Art",
                School = "Art Academy"
            };

            _repository.Update(updatedEducation);
            await _dbContext.SaveChangesAsync();

            var result = await _dbContext.Educations.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            Assert.NotNull(result);
            Assert.Equal("Master's", result.Degree);
            Assert.Equal("Fine Art", result.FieldOfStudy);
            Assert.Equal("Art Academy", result.School);
        }

        [Fact]
        public async Task Update_SetsEntityStateToModified()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "History",
                School = "State University"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            var updatedEducation = new Education
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "History",
                School = "State University"
            };

            _repository.Update(updatedEducation);

            var entry = _dbContext.Entry(updatedEducation);
            Assert.Equal(EntityState.Modified, entry.State);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_RemovesEntity()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Biology",
                School = "University of Biology"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education);
            await _dbContext.SaveChangesAsync();

            var result = await _dbContext.Educations.FindAsync(id);
            Assert.Null(result);
        }

        [Fact]
        public async Task Delete_SetsEntityStateToDeleted()
        {
            var id = Guid.NewGuid();
            var education = new Education
            {
                Id = id,
                Degree = "PhD",
                FieldOfStudy = "Linguistics",
                School = "Language Institute"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education);

            var entry = _dbContext.Entry(education);
            Assert.Equal(EntityState.Deleted, entry.State);
        }

        [Fact]
        public async Task Delete_DoesNotAffectOtherRecords()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var education1 = new Education
            {
                Id = id1,
                Degree = "Bachelor's",
                FieldOfStudy = "Economics",
                School = "Business School"
            };
            var education2 = new Education
            {
                Id = id2,
                Degree = "Master's",
                FieldOfStudy = "Finance",
                School = "Finance Academy"
            };

            _dbContext.Educations.AddRange(education1, education2);
            await _dbContext.SaveChangesAsync();

            _repository.Delete(education1);
            await _dbContext.SaveChangesAsync();

            var remaining = await _dbContext.Educations.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(id2, remaining[0].Id);
        }

        #endregion
    }
}
