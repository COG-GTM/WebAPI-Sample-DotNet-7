using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Controllers;

namespace WebApi.Tests;

public class EducationsControllerTests
{
    private readonly IEducationService _educationService = A.Fake<IEducationService>();

    [Fact]
    public async Task Get_Returns_Ok_With_Data()
    {
        var data = new[] { ValidEducation(), ValidEducation() };
        A.CallTo(() => _educationService.GetAll()).Returns(data);
        var controller = CreateController(_educationService);

        var result = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(data, ok.Value);
    }

    [Fact]
    public async Task GetById_Returns_Ok_When_Found()
    {
        var education = ValidEducation();
        A.CallTo(() => _educationService.GetById(education.Id)).Returns(education);
        var controller = CreateController(_educationService);

        var result = await controller.Get(education.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(education, ok.Value);
    }

    [Fact]
    public async Task GetById_Returns_NotFound_Problem_When_Missing()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _educationService.GetById(id)).Returns((EducationDto?)null);
        var controller = CreateController(_educationService);

        var result = await controller.Get(id);

        AssertProblem(result, 404, "Education not found.");
    }

    [Fact]
    public async Task Post_Returns_Created_With_Location()
    {
        var model = ValidEducation();
        var created = ValidEducation();
        created.Id = Guid.NewGuid();
        A.CallTo(() => _educationService.Add(model)).Returns(created);
        var controller = CreateController(_educationService);

        var result = await controller.Post(model);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(EducationsController.Get), createdResult.ActionName);
        Assert.Equal(created.Id, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task Post_Returns_Validation_Problem_When_ModelState_Is_Invalid()
    {
        var controller = CreateController(_educationService);
        controller.ModelState.AddModelError(nameof(EducationDto.Degree), "The Degree field is required.");

        var result = await controller.Post(ValidEducation());

        var badRequest = Assert.IsAssignableFrom<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        Assert.Contains(nameof(EducationDto.Degree), problem.Errors.Keys);
    }

    [Fact]
    public async Task Put_Returns_Validation_Problem_When_ModelState_Is_Invalid()
    {
        var controller = CreateController(_educationService);
        controller.ModelState.AddModelError(nameof(EducationDto.School), "The School field is required.");

        var result = await controller.Put(Guid.NewGuid(), ValidEducation());

        var badRequest = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    [Fact]
    public async Task Put_Returns_BadRequest_Problem_When_Ids_Do_Not_Match()
    {
        var routeId = Guid.NewGuid();
        var model = ValidEducation();
        model.Id = Guid.NewGuid();
        var controller = CreateController(_educationService);

        var result = await controller.Put(routeId, model);

        AssertProblem(result, 400, "Route id and body id do not match.");
    }

    [Fact]
    public async Task Put_Returns_NotFound_Problem_When_Update_Fails()
    {
        var id = Guid.NewGuid();
        var model = ValidEducation();
        model.Id = id;
        A.CallTo(() => _educationService.Update(id, model)).Returns(false);
        var controller = CreateController(_educationService);

        var result = await controller.Put(id, model);

        AssertProblem(result, 404, "Education not found.");
    }

    [Fact]
    public async Task Put_Returns_NoContent_When_Update_Succeeds()
    {
        var id = Guid.NewGuid();
        var model = ValidEducation();
        model.Id = id;
        A.CallTo(() => _educationService.Update(id, model)).Returns(true);
        var controller = CreateController(_educationService);

        var result = await controller.Put(id, model);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_Returns_NotFound_Problem_When_Delete_Fails()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _educationService.Delete(id)).Returns(false);
        var controller = CreateController(_educationService);

        var result = await controller.Delete(id);

        AssertProblem(result, 404, "Education not found.");
    }

    [Fact]
    public async Task Delete_Returns_NoContent_When_Delete_Succeeds()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _educationService.Delete(id)).Returns(true);
        var controller = CreateController(_educationService);

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    private static EducationDto ValidEducation()
    {
        return new EducationDto
        {
            Id = Guid.NewGuid(),
            Degree = "Bachelor",
            FieldOfStudy = "Computer Science",
            School = "Sample University",
            Description = "A valid education record."
        };
    }

    private static EducationsController CreateController(IEducationService service)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();

        return new EducationsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = provider
                }
            }
        };
    }

    private static void AssertProblem(IActionResult result, int status, string title)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(status, problem.Status);
        Assert.Equal(title, problem.Title);
    }
}
