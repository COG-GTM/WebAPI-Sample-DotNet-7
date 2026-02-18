using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests
{
    public class EducationsControllerTests
    {
        private readonly IEducationService educationService;
        public EducationsControllerTests()
        {
            educationService = A.Fake<IEducationService>();
        }

        #region Get
        [Fact]
        public async Task Get_With_Data_Returns_Ok()
        {
            // Arrange            
            var dummyEducationData = A.CollectionOfDummy<EducationDto>(3);
            A.CallTo(() => educationService.GetAll()).Returns(dummyEducationData);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<EducationDto>>(okResult.Value);
            Assert.Equal(3, data.Count);
        }

        [Fact]
        public async Task Get_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.GetAll()).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Get();

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetById_With_Valid_Id_Returns_Ok()
        {
            // Arrange
            var expectedDto = A.Dummy<EducationDto>();
            var validId = expectedDto.Id;
            A.CallTo(() => educationService.GetById(validId)).Returns(expectedDto);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Get(validId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            var id = Guid.NewGuid();
            A.CallTo(() => educationService.GetById(id)).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Get(id);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Post
        [Fact]
        public async Task Post_With_Valid_Model_Returns_Ok()
        {
            // Arrange
            var validEducationDto = A.Dummy<EducationDto>();
            A.CallTo(() => educationService.Add(validEducationDto)).Returns(validEducationDto);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Post(validEducationDto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Post_With_Null_Input_Returns_BadRequest()
        {
            // Arrange
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Post(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Post_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.Add(A<EducationDto>._)).Throws(new Exception());
            var controller = new EducationsController(educationService);
            var validEducationDto = A.Dummy<EducationDto>();

            // Act
            var result = await controller.Post(validEducationDto);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Put
        [Fact]
        public async Task Put_For_Successful_Update_Returns_Ok()
        {
            // Arrange
            A.CallTo(() => educationService.Update(A<Guid>._, A<EducationDto>._)).Returns(true);
            var controller = new EducationsController(educationService);
            var existingId = Guid.NewGuid();
            var educationDto = A.Dummy<EducationDto>();

            // Act
            var result = await controller.Put(existingId, educationDto);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Put_For_Failed_Update_Returns_BadRequest()
        {
            // Arrange
            A.CallTo(() => educationService.Update(A<Guid>._, A<EducationDto>._)).Returns(false);
            var controller = new EducationsController(educationService);
            var existingId = Guid.NewGuid();
            var educationDto = A.Dummy<EducationDto>();

            // Act
            var result = await controller.Put(existingId, educationDto);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Put_With_Null_Input_Returns_BadRequest()
        {
            // Arrange          
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Put(Guid.NewGuid(), null);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Put_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.Update(A<Guid>._, A<EducationDto>._)).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Put(Guid.NewGuid(), A.Dummy<EducationDto>());

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Delete
        [Fact]
        public async Task Delete_For_Successful_Deletion_Returns_Ok()
        {
            // Arrange
            A.CallTo(() => educationService.Delete(A<Guid>._)).Returns(true);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Delete_For_Failed_Deletion_Returns_BadRequest()
        {
            // Arrange
            A.CallTo(() => educationService.Delete(A<Guid>._)).Returns(false);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Delete_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.Delete(A<Guid>._)).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Delete(Guid.NewGuid());

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Statistics
        [Fact]
        public async Task GetStatistics_Returns_Ok()
        {
            // Arrange
            var stats = A.Dummy<EducationStatisticsDto>();
            A.CallTo(() => educationService.GetStatistics()).Returns(stats);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.GetStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsAssignableFrom<EducationStatisticsDto>(okResult.Value);
        }

        [Fact]
        public async Task GetStatistics_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.GetStatistics()).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.GetStatistics();

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Search
        [Fact]
        public async Task Search_With_Valid_Query_Returns_Ok()
        {
            // Arrange
            var dummyData = A.CollectionOfDummy<EducationDto>(2);
            A.CallTo(() => educationService.Search("Computer")).Returns(dummyData);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Search("Computer");

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Search_With_Empty_Query_Returns_BadRequest()
        {
            // Arrange
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Search("");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Search_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.Search(A<string>._)).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Search("test");

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Timeline
        [Fact]
        public async Task GetTimeline_Returns_Ok()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dummyData = A.CollectionOfDummy<EducationDto>(2);
            A.CallTo(() => educationService.GetTimelineByUserId(userId)).Returns(dummyData);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.GetTimeline(userId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetTimeline_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.GetTimelineByUserId(A<Guid>._)).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.GetTimeline(Guid.NewGuid());

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Export
        [Fact]
        public async Task Export_With_Empty_Format_Returns_BadRequest()
        {
            // Arrange
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Export("");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Export_With_Unsupported_Format_Returns_BadRequest()
        {
            // Arrange
            var dummyData = A.CollectionOfDummy<EducationDto>(1);
            A.CallTo(() => educationService.GetAll()).Returns(dummyData);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Export("xml");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Export_Csv_Returns_FileResult()
        {
            // Arrange
            var dummyData = A.CollectionOfDummy<EducationDto>(1);
            A.CallTo(() => educationService.GetAll()).Returns(dummyData);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Export("csv");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv", fileResult.ContentType);
        }

        [Fact]
        public async Task Export_Json_Returns_FileResult()
        {
            // Arrange
            var dummyData = A.CollectionOfDummy<EducationDto>(1);
            A.CallTo(() => educationService.GetAll()).Returns(dummyData);
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Export("json");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/json", fileResult.ContentType);
        }

        [Fact]
        public async Task Export_On_Exception_Returns_InternalServerError()
        {
            // Arrange
            A.CallTo(() => educationService.GetAll()).Throws(new Exception());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Export("csv");

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region Import
        [Fact]
        public async Task Import_With_Null_File_Returns_BadRequest()
        {
            // Arrange
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Import(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Import_With_Unsupported_Format_Returns_BadRequest()
        {
            // Arrange
            var fileMock = A.Fake<IFormFile>();
            A.CallTo(() => fileMock.FileName).Returns("test.xml");
            A.CallTo(() => fileMock.Length).Returns(100);
            A.CallTo(() => fileMock.OpenReadStream()).Returns(new MemoryStream());
            var controller = new EducationsController(educationService);

            // Act
            var result = await controller.Import(fileMock);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion
    }
}
