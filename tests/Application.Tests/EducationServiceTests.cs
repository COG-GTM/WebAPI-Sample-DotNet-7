using Application.Dtos;
using Application.Interfaces;
using Application.Service;
using Domain.Entities;
using Domain.Interfaces;
using FakeItEasy;
using Mapster;

namespace Application.Tests
{
    public class EducationServiceTests
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IEducationRepository educationRepository;

        public EducationServiceTests()
        {
            unitOfWork = A.Fake<IUnitOfWork>();
            educationRepository = A.Fake<IEducationRepository>();
            A.CallTo(() => unitOfWork.Education).Returns(educationRepository);
        }

        [Fact]
        public async Task GetAll_With_Data_Returns_List_Of_EducationDto()
        {
            // Arrange
            var educationData = new List<Education>();
            A.CallTo(() => educationRepository.GetAll()).Returns(educationData);
            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<EducationDto>>(result);
            Assert.Equal(educationData.Count, result.Count());
        }

        [Fact]
        public async Task GetById_With_Data_Returns_EducationDto()
        {
            // Arrange
            var educationId = Guid.NewGuid();
            var educationData = A.Dummy<Education>();
            A.CallTo(() => educationRepository.GetById(educationId)).Returns(educationData);
            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.GetById(educationId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<EducationDto>(result);
        }

        [Fact]
        public async Task Add_Returns_Added_EducationDto()
        {
            // Arrange
            var educationDto = A.Dummy<EducationDto>();
            var addedEducation = educationDto.Adapt<Education>();
            addedEducation.Id = Guid.NewGuid();
            A.CallTo(() => educationRepository.Add(A<Education>._)).Returns(addedEducation);

            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.Add(educationDto);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<EducationDto>(result);
            Assert.Equal(addedEducation.Id, result.Id);
        }

        [Fact]
        public async Task Update_For_Successful_Update_Returns_True()
        {
            // Arrange
            var existingId = Guid.NewGuid();
            var existingEducation = A.Dummy<Education>();
            existingEducation.Id = existingId;
            A.CallTo(() => educationRepository.GetById(existingId)).Returns(existingEducation);

            var updatedEducationDto = A.Dummy<EducationDto>();
            updatedEducationDto.Id = existingId;
            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.Update(existingId, updatedEducationDto);

            // Assert
            Assert.True(result);
            A.CallTo(() => unitOfWork.Education.Update(A<Education>.That.Matches(e => e.Id == existingId))).MustHaveHappenedOnceExactly();
            A.CallTo(() => unitOfWork.SaveChangesAsync(default)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Update_For_Id_Mismatch_Returns_False()
        {
            // Arrange
            var educationService = new EducationService(unitOfWork);
            var invalidEducationDto = A.Dummy<EducationDto>();
            var existingId = Guid.NewGuid();

            // Act
            var result = await educationService.Update(existingId, invalidEducationDto);

            // Assert
            Assert.False(result);
            A.CallTo(() => unitOfWork.Education.GetById(A<Guid>._)).MustNotHaveHappened();
            A.CallTo(() => unitOfWork.Education.Update(A<Education>._)).MustNotHaveHappened();
            A.CallTo(() => unitOfWork.SaveChangesAsync(default)).MustNotHaveHappened();
        }

        [Fact]
        public async Task Delete_With_Existing_Id_Returns_True()
        {
            // Arrange
            var existingId = Guid.NewGuid();
            var existingEducation = A.Dummy<Education>();
            A.CallTo(() => educationRepository.GetById(existingId)).Returns(existingEducation);

            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.Delete(existingId);

            // Assert
            Assert.True(result);
            A.CallTo(() => unitOfWork.Education.Delete(existingEducation)).MustHaveHappenedOnceExactly();
            A.CallTo(() => unitOfWork.SaveChangesAsync(default)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetStatistics_Returns_EducationStatisticsDto()
        {
            // Arrange
            var educationData = new List<Education>
            {
                new Education { Id = Guid.NewGuid(), Degree = "Bachelor's", School = "MIT", FieldOfStudy = "CS" },
                new Education { Id = Guid.NewGuid(), Degree = "Bachelor's", School = "MIT", FieldOfStudy = "Math" },
                new Education { Id = Guid.NewGuid(), Degree = "Master's", School = "Stanford", FieldOfStudy = "CS" }
            };
            A.CallTo(() => educationRepository.GetAll()).Returns(educationData);
            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.GetStatistics();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.DegreeCounts["Bachelor's"]);
            Assert.Equal(1, result.DegreeCounts["Master's"]);
            Assert.Equal("MIT", result.MostCommonSchools[0].School);
        }

        [Fact]
        public async Task Search_Returns_Matching_Results()
        {
            // Arrange
            var educationData = new List<Education>
            {
                new Education { Id = Guid.NewGuid(), Degree = "Bachelor's", School = "MIT", FieldOfStudy = "Computer Science" },
                new Education { Id = Guid.NewGuid(), Degree = "Master's", School = "Stanford", FieldOfStudy = "Mathematics" }
            };
            A.CallTo(() => educationRepository.GetAll()).Returns(educationData);
            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.Search("Computer");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task Search_With_No_Match_Returns_Empty()
        {
            // Arrange
            var educationData = new List<Education>
            {
                new Education { Id = Guid.NewGuid(), Degree = "Bachelor's", School = "MIT", FieldOfStudy = "CS" }
            };
            A.CallTo(() => educationRepository.GetAll()).Returns(educationData);
            var educationService = new EducationService(unitOfWork);

            // Act
            var result = await educationService.Search("Nonexistent");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTimelineByUserId_Returns_Sorted_By_StartDate()
        {
            // Arrange
            var educationData = new List<Education>
            {
                new Education { Id = Guid.NewGuid(), Degree = "Master's", School = "Stanford", FieldOfStudy = "CS", StartDate = new DateTime(2020, 1, 1) },
                new Education { Id = Guid.NewGuid(), Degree = "Bachelor's", School = "MIT", FieldOfStudy = "CS", StartDate = new DateTime(2016, 1, 1) }
            };
            A.CallTo(() => educationRepository.GetAll()).Returns(educationData);
            var educationService = new EducationService(unitOfWork);

            // Act
            var result = (await educationService.GetTimelineByUserId(Guid.NewGuid())).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.True(result[0].StartDate < result[1].StartDate);
        }
    }
}
