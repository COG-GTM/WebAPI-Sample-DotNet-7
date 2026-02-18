using Application.Dtos;

namespace Application.Service.Interfaces
{
    public interface IEducationService
    {
        Task<IEnumerable<EducationDto>> GetAll();
        Task<EducationDto?> GetById(Guid id);
        Task<EducationDto> Add(EducationDto model);
        Task<bool> Update(Guid id, EducationDto model);
        Task<bool> Delete(Guid id);
        Task<EducationStatisticsDto> GetStatistics();
        Task<IEnumerable<EducationDto>> Search(string query);
        Task<IEnumerable<EducationDto>> GetTimelineByUserId(Guid userId);
    }
}
