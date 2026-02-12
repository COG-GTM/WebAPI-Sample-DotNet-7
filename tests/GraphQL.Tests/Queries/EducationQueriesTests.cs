using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using WebApi.GraphQL.Queries;

namespace GraphQL.Tests.Queries;

public class EducationQueriesTests
{
    private readonly IEducationService _educationService;
    private readonly EducationQueries _queries;

    public EducationQueriesTests()
    {
        _educationService = A.Fake<IEducationService>();
        _queries = new EducationQueries();
    }

    [Fact]
    public async Task GetEducations_Returns_All_Education_Records()
    {
        var expectedEducations = A.CollectionOfDummy<EducationDto>(3).ToList();
        A.CallTo(() => _educationService.GetAll()).Returns(expectedEducations);

        var result = await _queries.GetEducations(_educationService);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        A.CallTo(() => _educationService.GetAll()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetEducation_With_Valid_Id_Returns_Education()
    {
        var educationId = Guid.NewGuid();
        var expectedEducation = A.Dummy<EducationDto>();
        expectedEducation.Id = educationId;
        A.CallTo(() => _educationService.GetById(educationId)).Returns(expectedEducation);

        var result = await _queries.GetEducation(educationId, _educationService);

        Assert.NotNull(result);
        Assert.Equal(educationId, result.Id);
        A.CallTo(() => _educationService.GetById(educationId)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetEducation_With_Invalid_Id_Returns_Null()
    {
        var invalidId = Guid.NewGuid();
        A.CallTo(() => _educationService.GetById(invalidId)).Returns((EducationDto?)null);

        var result = await _queries.GetEducation(invalidId, _educationService);

        Assert.Null(result);
    }
}
