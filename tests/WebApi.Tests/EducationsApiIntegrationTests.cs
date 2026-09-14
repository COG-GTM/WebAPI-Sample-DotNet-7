using System.Net;
using System.Net.Http.Json;
using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WebApi.Tests;

public class EducationsApiIntegrationTests : IDisposable
{
    private readonly IEducationService _educationService = A.Fake<IEducationService>();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EducationsApiIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Production");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEducationService>();
                services.AddSingleton<IEducationService>(_educationService);
            });
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Post_Invalid_Model_Returns_Validation_Problem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/educations",
            new
            {
                FieldOfStudy = "Computer Science",
                School = new string('s', 251)
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Degree", problem!.Errors.Keys);
        Assert.Contains("School", problem.Errors.Keys);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task Get_Unknown_Id_Returns_NotFound_Problem()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _educationService.GetById(id)).Returns((EducationDto?)null);

        var response = await _client.GetAsync($"/api/educations/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Education not found.", problem!.Title);
    }

    [Fact]
    public async Task Get_When_Service_Throws_Returns_Unexpected_Error_Problem()
    {
        A.CallTo(() => _educationService.GetAll()).Throws(new InvalidOperationException("secret exception"));

        var response = await _client.GetAsync("/api/educations");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var body = await response.Content.ReadAsStringAsync();
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("An unexpected error occurred.", problem!.Title);
        Assert.DoesNotContain("secret exception", body);
    }

    [Fact]
    public async Task Post_Valid_Model_Returns_Created_With_Location()
    {
        var model = ValidEducation();
        var created = ValidEducation();
        created.Id = Guid.NewGuid();
        A.CallTo(() => _educationService.Add(A<EducationDto>._)).Returns(created);

        var response = await _client.PostAsJsonAsync("/api/educations", model);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(created.Id.ToString(), response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Put_Mismatched_Id_Returns_BadRequest_Problem()
    {
        var routeId = Guid.NewGuid();
        var model = ValidEducation();
        model.Id = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync($"/api/educations/{routeId}", model);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Route id and body id do not match.", problem!.Title);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
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
}
