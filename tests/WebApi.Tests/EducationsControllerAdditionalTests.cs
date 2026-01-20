using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests
{
    public class EducationsControllerAdditionalTests
    {
        private readonly IEducationService _educationService;

        public EducationsControllerAdditionalTests()
        {
            _educationService = A.Fake<IEducationService>();
        }

        #region Get - Additional Tests

        [Fact]
        public async Task Get_With_Empty_List_Returns_Ok_With_Empty_Collection()
        {
            // Arrange
            var emptyList = new List<EducationDto>();
            A.CallTo(() => _educationService.GetAll()).Returns(emptyList);
            var controller = new EducationsController(_educationService);

            // Act
            var result = await controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<EducationDto>>(okResult.Value);
            Assert.Empty(data);
        }

        [Fact]
        public async Task Get_Calls_Service_GetAll_Once()
        {
            // Arrange
            var dummyData = A.CollectionOfDummy<EducationDto>(2);
            A.CallTo(() => _educationService.GetAll()).Returns(dummyData);
            var controller = new EducationsController(_educationService);

            // Act
            await controller.Get();

            // Assert
            A.CallTo(() => _educationService.GetAll()).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region GetById - Additional Tests

        [Fact]
        public async Task GetById_With_NonExistent_Id_Returns_NoContent()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            A.CallTo(() => _educationService.GetById(nonExistentId)).Returns((EducationDto?)null);
            var controller = new EducationsController(_educationService);

            // Act
            var result = await controller.Get(nonExistentId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetById_Calls_Service_GetById_With_Correct_Id()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var expectedDto = new EducationDto
            {
                Id = expectedId,
                Degree = "Bachelor's",
                FieldOfStudy = "Computer Science",
                School = "MIT"
            };
            A.CallTo(() => _educationService.GetById(expectedId)).Returns(expectedDto);
            var controller = new EducationsController(_educationService);

            // Act
            await controller.Get(expectedId);

            // Assert
            A.CallTo(() => _educationService.GetById(expectedId)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetById_Returns_Correct_Education_Data()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var expectedDto = new EducationDto
            {
                Id = expectedId,
                Degree = "Master's",
                FieldOfStudy = "Data Science",
                School = "Stanford",
                Description = "Graduate program"
            };
            A.CallTo(() => _educationService.GetById(expectedId)).Returns(expectedDto);
            var controller = new EducationsController(_educationService);

            // Act
            var result = await controller.Get(expectedId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedDto = Assert.IsType<EducationDto>(okResult.Value);
            Assert.Equal(expectedId, returnedDto.Id);
            Assert.Equal("Master's", returnedDto.Degree);
            Assert.Equal("Data Science", returnedDto.FieldOfStudy);
            Assert.Equal("Stanford", returnedDto.School);
            Assert.Equal("Graduate program", returnedDto.Description);
        }

        #endregion

        #region Post - Additional Tests

        [Fact]
        public async Task Post_Calls_Service_Add_With_Correct_Model()
        {
            // Arrange
            var inputDto = new EducationDto
            {
                Id = Guid.NewGuid(),
                Degree = "PhD",
                FieldOfStudy = "Physics",
                School = "Caltech"
            };
            A.CallTo(() => _educationService.Add(inputDto)).Returns(inputDto);
            var controller = new EducationsController(_educationService);

            // Act
            await controller.Post(inputDto);

            // Assert
            A.CallTo(() => _educationService.Add(inputDto)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Post_Returns_Created_Education_Data()
        {
            // Arrange
            var inputDto = new EducationDto
            {
                Id = Guid.Empty,
                Degree = "Associate's",
                FieldOfStudy = "Business",
                School = "Community College"
            };
            var returnedDto = new EducationDto
            {
                Id = Guid.NewGuid(),
                Degree = "Associate's",
                FieldOfStudy = "Business",
                School = "Community College"
            };
            A.CallTo(() => _educationService.Add(inputDto)).Returns(returnedDto);
            var controller = new EducationsController(_educationService);

            // Act
            var result = await controller.Post(inputDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<EducationDto>(okResult.Value);
            Assert.NotEqual(Guid.Empty, data.Id);
            Assert.Equal("Associate's", data.Degree);
        }

        [Fact]
        public async Task Post_With_Invalid_ModelState_Returns_BadRequest()
        {
            // Arrange
            var controller = new EducationsController(_educationService);
            controller.ModelState.AddModelError("Degree", "Degree is required");
            var inputDto = new EducationDto
            {
                Id = Guid.NewGuid(),
                Degree = "",
                FieldOfStudy = "Science",
                School = "University"
            };

            // Act
            var result = await controller.Post(inputDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Put - Additional Tests

        [Fact]
        public async Task Put_Calls_Service_Update_With_Correct_Parameters()
        {
            // Arrange
            var id = Guid.NewGuid();
            var inputDto = new EducationDto
            {
                Id = id,
                Degree = "Bachelor's",
                FieldOfStudy = "Engineering",
                School = "MIT"
            };
            A.CallTo(() => _educationService.Update(id, inputDto)).Returns(true);
            var controller = new EducationsController(_educationService);

            // Act
            await controller.Put(id, inputDto);

            // Assert
            A.CallTo(() => _educationService.Update(id, inputDto)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Put_With_Invalid_ModelState_Returns_BadRequest()
        {
            // Arrange
            var controller = new EducationsController(_educationService);
            controller.ModelState.AddModelError("School", "School is required");
            var id = Guid.NewGuid();
            var inputDto = new EducationDto
            {
                Id = id,
                Degree = "Master's",
                FieldOfStudy = "Chemistry",
                School = ""
            };

            // Act
            var result = await controller.Put(id, inputDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Put_With_Mismatched_Id_Still_Uses_Route_Id()
        {
            // Arrange
            var routeId = Guid.NewGuid();
            var bodyId = Guid.NewGuid();
            var inputDto = new EducationDto
            {
                Id = bodyId,
                Degree = "Doctorate",
                FieldOfStudy = "Medicine",
                School = "Johns Hopkins"
            };
            A.CallTo(() => _educationService.Update(routeId, inputDto)).Returns(true);
            var controller = new EducationsController(_educationService);

            // Act
            await controller.Put(routeId, inputDto);

            // Assert
            A.CallTo(() => _educationService.Update(routeId, inputDto)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _educationService.Update(bodyId, A<EducationDto>._)).MustNotHaveHappened();
        }

        #endregion

        #region Delete - Additional Tests

        [Fact]
        public async Task Delete_Calls_Service_Delete_With_Correct_Id()
        {
            // Arrange
            var id = Guid.NewGuid();
            A.CallTo(() => _educationService.Delete(id)).Returns(true);
            var controller = new EducationsController(_educationService);

            // Act
            await controller.Delete(id);

            // Assert
            A.CallTo(() => _educationService.Delete(id)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Delete_With_Empty_Guid_Calls_Service_With_Empty_Guid()
        {
            // Arrange
            var emptyGuid = Guid.Empty;
            A.CallTo(() => _educationService.Delete(emptyGuid)).Returns(false);
            var controller = new EducationsController(_educationService);

            // Act
            var result = await controller.Delete(emptyGuid);

            // Assert
            A.CallTo(() => _educationService.Delete(emptyGuid)).MustHaveHappenedOnceExactly();
            Assert.IsType<BadRequestResult>(result);
        }

        #endregion
    }
}
