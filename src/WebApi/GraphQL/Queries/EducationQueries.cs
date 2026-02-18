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

    public async Task<EducationDto?> GetEducationById(
        Guid id,
        [Service] IEducationService educationService)
    {
        return await educationService.GetById(id);
    }

    public async Task<EducationStatisticsDto> GetEducationStatistics(
        [Service] IEducationService educationService)
    {
        return await educationService.GetStatistics();
    }

    public async Task<IEnumerable<EducationDto>> SearchEducations(
        string query,
        [Service] IEducationService educationService)
    {
        return await educationService.Search(query);
    }

    public async Task<IEnumerable<EducationDto>> GetEducationTimeline(
        Guid userId,
        [Service] IEducationService educationService)
    {
        return await educationService.GetTimelineByUserId(userId);
    }
}
