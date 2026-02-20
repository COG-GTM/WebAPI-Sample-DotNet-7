using Domain.Entities;
using Infrastructure.DbContexts;
using Infrastructure.Repositories;
using Infrastructure.UoW;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.UoW
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
        public void Constructor_Education_Is_EducationRepository_Instance()
        {
            Assert.IsType<EducationRepository>(_unitOfWork.Education);
        }

        [Fact]
        public async Task SaveChangesAsync_Returns_Zero_When_No_Changes()
        {
            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task SaveChangesAsync_Returns_Count_Of_Changes()
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
        public async Task SaveChangesAsync_Persists_Entity_To_Database()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "PhD",
                FieldOfStudy = "Mathematics",
                School = "MIT"
            };

            await _dbContext.Educations.AddAsync(education);
            await _unitOfWork.SaveChangesAsync();

            var saved = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(saved);
            Assert.Equal("PhD", saved.Degree);
        }

        [Fact]
        public async Task SaveChangesAsync_Accepts_CancellationToken()
        {
            using var cts = new CancellationTokenSource();
            var result = await _unitOfWork.SaveChangesAsync(cts.Token);

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task SaveChangesAsync_With_Default_CancellationToken_Succeeds()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's degree",
                FieldOfStudy = "Physics",
                School = "Stanford"
            };

            await _dbContext.Educations.AddAsync(education);

            var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SaveChangesAsync_With_Multiple_Entities_Returns_Correct_Count()
        {
            var education1 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "CS",
                School = "School A"
            };
            var education2 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's",
                FieldOfStudy = "AI",
                School = "School B"
            };

            await _dbContext.Educations.AddRangeAsync(education1, education2);

            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public void Dispose_Does_Not_Throw()
        {
            var options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new SampleDbContext(options);
            var uow = new UnitOfWork(dbContext);

            var exception = Record.Exception(() => uow.Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new SampleDbContext(options);
            var uow = new UnitOfWork(dbContext);

            var exception = Record.Exception(() =>
            {
                uow.Dispose();
                uow.Dispose();
            });

            Assert.Null(exception);
        }

        [Fact]
        public async Task Education_Repository_Can_Add_And_Retrieve()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Associate's",
                FieldOfStudy = "Nursing",
                School = "Community College"
            };

            await _unitOfWork.Education.Add(education);
            await _unitOfWork.SaveChangesAsync();

            var all = await _unitOfWork.Education.GetAll();
            Assert.Contains(all, e => e.Id == education.Id);
        }

        [Fact]
        public async Task Education_Repository_GetById_After_Save()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Doctorate",
                FieldOfStudy = "Chemistry",
                School = "Oxford"
            };

            await _unitOfWork.Education.Add(education);
            await _unitOfWork.SaveChangesAsync();

            var found = await _unitOfWork.Education.GetById(education.Id);
            Assert.NotNull(found);
            Assert.Equal("Doctorate", found.Degree);
        }

        [Fact]
        public async Task SaveChangesAsync_After_Update_Persists_Changes()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's",
                FieldOfStudy = "Biology",
                School = "Harvard"
            };

            await _dbContext.Educations.AddAsync(education);
            await _unitOfWork.SaveChangesAsync();

            education.Degree = "Master's";
            _unitOfWork.Education.Update(education);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(updated);
            Assert.Equal("Master's", updated.Degree);
        }

        [Fact]
        public async Task SaveChangesAsync_After_Delete_Removes_Entity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "MBA",
                FieldOfStudy = "Business",
                School = "Wharton"
            };

            await _dbContext.Educations.AddAsync(education);
            await _unitOfWork.SaveChangesAsync();

            _unitOfWork.Education.Delete(education);
            await _unitOfWork.SaveChangesAsync();

            var deleted = await _dbContext.Educations.FindAsync(education.Id);
            Assert.Null(deleted);
        }

        public void Dispose()
        {
            try
            {
                _dbContext.Database.EnsureDeleted();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                _dbContext.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
