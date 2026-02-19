using Domain.Entities;
using Infrastructure.DbContexts;
using Infrastructure.Repositories;
using Infrastructure.UoW;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests
{
    public class UnitOfWorkTests : IDisposable
    {
        private readonly SampleDbContext _dbContext;
        private readonly UnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            var options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new SampleDbContext(options);
            _unitOfWork = new UnitOfWork(_dbContext);
        }

        [Fact]
        public void Constructor_Initializes_Education_Repository()
        {
            Assert.NotNull(_unitOfWork.Education);
        }

        [Fact]
        public void Education_Property_Returns_EducationRepository_Instance()
        {
            var repo = _unitOfWork.Education;

            Assert.IsAssignableFrom<Domain.Interfaces.IEducationRepository>(repo);
        }

        [Fact]
        public async Task SaveChangesAsync_Persists_Changes_To_Database()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's degree",
                FieldOfStudy = "Computer Science",
                School = "Test University"
            };

            await _dbContext.Educations.AddAsync(education);
            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SaveChangesAsync_Returns_Zero_When_No_Changes()
        {
            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task SaveChangesAsync_With_CancellationToken_Persists_Changes()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "PhD",
                FieldOfStudy = "Mathematics",
                School = "Another University"
            };

            using var cts = new CancellationTokenSource();
            await _dbContext.Educations.AddAsync(education);
            var result = await _unitOfWork.SaveChangesAsync(cts.Token);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SaveChangesAsync_Persists_Multiple_Entities()
        {
            var education1 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "Physics",
                School = "University A"
            };
            var education2 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's",
                FieldOfStudy = "Chemistry",
                School = "University B"
            };

            await _dbContext.Educations.AddRangeAsync(education1, education2);
            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task SaveChangesAsync_After_Add_Via_Repository_Persists_Entity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Associate's",
                FieldOfStudy = "Art",
                School = "Community College"
            };

            await _unitOfWork.Education.Add(education);
            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(1, result);
            var saved = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(saved);
            Assert.Equal("Associate's", saved.Degree);
        }

        [Fact]
        public async Task SaveChangesAsync_After_Update_Via_Repository_Persists_Changes()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "Biology",
                School = "State University"
            };

            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();

            education.Degree = "Master's";
            _unitOfWork.Education.Update(education);
            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(1, result);
            var updated = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(updated);
            Assert.Equal("Master's", updated.Degree);
        }

        [Fact]
        public async Task SaveChangesAsync_After_Delete_Via_Repository_Removes_Entity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "PhD",
                FieldOfStudy = "Engineering",
                School = "Tech Institute"
            };

            await _dbContext.Educations.AddAsync(education);
            await _dbContext.SaveChangesAsync();

            _unitOfWork.Education.Delete(education);
            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(1, result);
            var deleted = await _dbContext.Educations.FindAsync(education.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public void Dispose_Does_Not_Throw()
        {
            var options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var dbContext = new SampleDbContext(options);
            var unitOfWork = new UnitOfWork(dbContext);

            var exception = Record.Exception(() => unitOfWork.Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var dbContext = new SampleDbContext(options);
            var unitOfWork = new UnitOfWork(dbContext);

            var exception = Record.Exception(() =>
            {
                unitOfWork.Dispose();
                unitOfWork.Dispose();
            });

            Assert.Null(exception);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
