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
        public void Constructor_Education_Repository_Is_EducationRepository()
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
        public async Task SaveChangesAsync_Returns_Count_Of_Changed_Entities()
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
        public async Task SaveChangesAsync_Persists_Added_Entity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "PhD",
                FieldOfStudy = "Mathematics",
                School = "Research University"
            };

            await _dbContext.Educations.AddAsync(education);
            await _unitOfWork.SaveChangesAsync();

            var saved = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(saved);
            Assert.Equal("PhD", saved.Degree);
        }

        [Fact]
        public async Task SaveChangesAsync_Persists_Multiple_Entities()
        {
            var education1 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's degree",
                FieldOfStudy = "Physics",
                School = "University A"
            };

            var education2 = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Master's degree",
                FieldOfStudy = "Chemistry",
                School = "University B"
            };

            await _dbContext.Educations.AddRangeAsync(education1, education2);

            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task SaveChangesAsync_With_CancellationToken_Completes()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Associate's degree",
                FieldOfStudy = "Nursing",
                School = "Community College"
            };

            await _dbContext.Educations.AddAsync(education);
            using var cts = new CancellationTokenSource();

            var result = await _unitOfWork.SaveChangesAsync(cts.Token);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SaveChangesAsync_With_Canceled_Token_Throws_OperationCanceledException()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Diploma",
                FieldOfStudy = "Culinary Arts",
                School = "Culinary Institute"
            };

            await _dbContext.Educations.AddAsync(education);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _unitOfWork.SaveChangesAsync(cts.Token));
        }

        [Fact]
        public async Task SaveChangesAsync_Persists_Updated_Entity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's degree",
                FieldOfStudy = "History",
                School = "State University"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            education.Degree = "Master's degree";
            _dbContext.Educations.Update(education);

            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(1, result);
            var updated = await _dbContext.Educations.FindAsync(education.Id);
            Assert.NotNull(updated);
            Assert.Equal("Master's degree", updated.Degree);
        }

        [Fact]
        public async Task SaveChangesAsync_Persists_Deleted_Entity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Certificate",
                FieldOfStudy = "Web Development",
                School = "Online Academy"
            };

            _dbContext.Educations.Add(education);
            await _dbContext.SaveChangesAsync();

            _dbContext.Educations.Remove(education);

            var result = await _unitOfWork.SaveChangesAsync();

            Assert.Equal(1, result);
            var deleted = await _dbContext.Educations.FindAsync(education.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Education_Repository_Can_Add_And_Retrieve_Entity()
        {
            var education = new Education
            {
                Id = Guid.NewGuid(),
                Degree = "Bachelor's degree",
                FieldOfStudy = "Art",
                School = "Art Institute"
            };

            await _unitOfWork.Education.Add(education);
            await _unitOfWork.SaveChangesAsync();

            var allEducations = await _unitOfWork.Education.GetAll();
            Assert.Contains(allEducations, e => e.Id == education.Id);
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

        [Fact]
        public void Multiple_UnitOfWork_Instances_Have_Independent_Repositories()
        {
            var options1 = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var options2 = new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext1 = new SampleDbContext(options1);
            using var dbContext2 = new SampleDbContext(options2);
            var uow1 = new UnitOfWork(dbContext1);
            var uow2 = new UnitOfWork(dbContext2);

            Assert.NotSame(uow1.Education, uow2.Education);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
