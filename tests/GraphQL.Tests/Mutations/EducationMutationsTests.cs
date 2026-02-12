using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using WebApi.GraphQL.Mutations;
using WebApi.GraphQL.Types;

namespace GraphQL.Tests.Mutations;

public class EducationMutationsTests
{
    private readonly IEducationService _educationService;
    private readonly EducationMutations _mutations;

    public EducationMutationsTests()
    {
        _educationService = A.Fake<IEducationService>();
        _mutations = new EducationMutations();
    }

    [Fact]
    public async Task CreateEducation_With_Valid_Input_Returns_Success_Payload()
    {
        var input = new CreateEducationInput(
            "Bachelor's degree",
            "Computer Science",
            "MIT",
            "Description");

        var createdEducation = new EducationDto
        {
            Id = Guid.NewGuid(),
            Degree = input.Degree,
            FieldOfStudy = input.FieldOfStudy,
            School = input.School,
            Description = input.Description
        };

        A.CallTo(() => _educationService.Add(A<EducationDto>._)).Returns(createdEducation);

        var result = await _mutations.CreateEducation(input, _educationService);

        Assert.NotNull(result.Education);
        Assert.Null(result.Errors);
        Assert.Equal(input.Degree, result.Education.Degree);
    }

    [Fact]
    public async Task DeleteEducation_With_Valid_Id_Returns_Success()
    {
        var educationId = Guid.NewGuid();
        A.CallTo(() => _educationService.Delete(educationId)).Returns(true);

        var result = await _mutations.DeleteEducation(educationId, _educationService);

        Assert.True(result.Success);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task DeleteEducation_With_Invalid_Id_Returns_Error()
    {
        var invalidId = Guid.NewGuid();
        A.CallTo(() => _educationService.Delete(invalidId)).Returns(false);

        var result = await _mutations.DeleteEducation(invalidId, _educationService);

        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "NOT_FOUND");
    }
}
