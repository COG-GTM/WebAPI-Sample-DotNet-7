using Application.Dtos;
using Application.Interfaces;
using Application.Service.Interfaces;
using Domain.Entities;
using Mapster;

namespace Application.Service
{
    public class EducationService : IEducationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public EducationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<EducationDto>> GetAll()
        {
            var result = await _unitOfWork.Education.GetAll();
            return result.Adapt<List<EducationDto>>();
        }

        public async Task<EducationDto?> GetById(Guid id)
        {
            var result = await _unitOfWork.Education.GetById(id);
            return result?.Adapt<EducationDto>();
        }

        public async Task<EducationDto> Add(EducationDto model)
        {
            Education toAdd = model.Adapt<Education>();
            toAdd.Id = Guid.NewGuid();
            var result = await _unitOfWork.Education.Add(toAdd);
            await _unitOfWork.SaveChangesAsync();
            return result.Adapt<EducationDto>();
        }

        public async Task<bool> Update(Guid id, EducationDto model)
        {
            if (model is null || model.Id != id) return false;

            var toUpdate = await _unitOfWork.Education.GetById(id);
            if (toUpdate is null) return false;

            toUpdate = model.Adapt<Education>();

            _unitOfWork.Education.Update(toUpdate);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Delete(Guid id)
        {
            var toDelete = await _unitOfWork.Education.GetById(id);
            if (toDelete is null) return false;

            _unitOfWork.Education.Delete(toDelete);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<EducationStatisticsDto> GetStatistics()
        {
            var allEducations = await _unitOfWork.Education.GetAll();
            var educationList = allEducations.ToList();

            var degreeCounts = educationList
                .Where(e => !string.IsNullOrEmpty(e.Degree))
                .GroupBy(e => e.Degree!)
                .ToDictionary(g => g.Key, g => g.Count());

            var mostCommonSchools = educationList
                .Where(e => !string.IsNullOrEmpty(e.School))
                .GroupBy(e => e.School!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new SchoolCountDto { School = g.Key, Count = g.Count() })
                .ToList();

            return new EducationStatisticsDto
            {
                TotalCount = educationList.Count,
                DegreeCounts = degreeCounts,
                MostCommonSchools = mostCommonSchools
            };
        }

        public async Task<IEnumerable<EducationDto>> Search(string query)
        {
            var allEducations = await _unitOfWork.Education.GetAll();
            var filtered = allEducations.Where(e =>
                (e.Degree != null && e.Degree.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (e.School != null && e.School.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (e.FieldOfStudy != null && e.FieldOfStudy.Contains(query, StringComparison.OrdinalIgnoreCase)));
            return filtered.Adapt<List<EducationDto>>();
        }

        public async Task<IEnumerable<EducationDto>> GetTimelineByUserId(Guid userId)
        {
            var allEducations = await _unitOfWork.Education.GetAll();
            var sorted = allEducations
                .OrderBy(e => e.StartDate)
                .ToList();
            return sorted.Adapt<List<EducationDto>>();
        }
    }
}
