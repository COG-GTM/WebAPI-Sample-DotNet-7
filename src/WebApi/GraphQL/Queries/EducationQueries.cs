using Application.Dtos;
using Application.Service.Interfaces;

namespace WebApi.GraphQL.Queries;

public class EducationQueries
{
    public async Task<IEnumerable<EducationDto>> GetEducations(
        [Service] IEducationService educationService)
    {
        return await educationService.GetAll();
    }

    public async Task<EducationDto?> GetEducation(
        Guid id,
        [Service] IEducationService educationService)
    {
        return await educationService.GetById(id);
    }
}
